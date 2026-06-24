using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Employees;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Employees;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;

namespace Oypa.Crm.Infrastructure.Features.Employees;

/// <summary>
/// Personel sorgularını, CRUD işlemlerini ve hesap yönetimini kapsayan servis.
/// UserManager'a ihtiyaç duyduğu için Infrastructure katmanında yer alır.
/// </summary>
public sealed class EmployeeService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IIdentityService identityService,
    IOrgScopeService orgScopeService,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IEmployeeService
{
    // -----------------------------------------------------------------------
    // Kapsam çözümü (IOrgScopeService'e delege edildi)
    // -----------------------------------------------------------------------

    private async Task<(bool AllEmployees, HashSet<Guid> Ids)> ResolveScopeAsync(CancellationToken cancellationToken)
    {
        var scope = await orgScopeService.ResolveAsync(cancellationToken);
        return (scope.AllEmployees, scope.Ids);
    }

    /// <summary>Hedef personelin çağıranın kapsamında olup olmadığını doğrular.</summary>
    private async Task EnsureInScopeAsync(Guid targetId, CancellationToken cancellationToken)
    {
        var (all, ids) = await ResolveScopeAsync(cancellationToken);
        if (!all && !ids.Contains(targetId))
            throw new ForbiddenAppException($"Personel (id: {targetId}) yönetim kapsamınızın dışında.");
    }

    // -----------------------------------------------------------------------
    // Sorgular
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var employees = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .ToListAsync(cancellationToken);

        return await MapToDtoListAsync(employees, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetManagedAsync(CancellationToken cancellationToken = default)
    {
        var (all, ids) = await ResolveScopeAsync(cancellationToken);

        IQueryable<Employee> query = db.Set<Employee>().AsNoTracking().Include(e => e.Manager);

        if (!all)
            query = query.Where(e => ids.Contains(e.Id));

        var employees = await query.ToListAsync(cancellationToken);
        return await MapToDtoListAsync(employees, cancellationToken);
    }

    public async Task<PagedResult<EmployeeDto>> GetManagedPagedAsync(
        PagedQuery pagedQuery,
        CancellationToken cancellationToken = default)
    {
        var (all, ids) = await ResolveScopeAsync(cancellationToken);

        IQueryable<Employee> query = db.Set<Employee>().AsNoTracking().Include(e => e.Manager);

        // Org kapsam filtresi — mevcut GetManagedAsync ile aynı kural
        if (!all)
            query = query.Where(e => ids.Contains(e.Id));

        // Serbest metin araması: ad, e-posta veya ünvan
        if (!string.IsNullOrWhiteSpace(pagedQuery.Search))
        {
            var term = pagedQuery.Search.Trim().ToLower();
            query = query.Where(e =>
                (e.FullName != null && e.FullName.ToLower().Contains(term)) ||
                (e.Email != null && e.Email.ToLower().Contains(term)) ||
                e.Title.ToLower().Contains(term));
        }

        // Toplam kayıt sayısı — sayfa kesmesinden önce hesaplanır
        var totalCount = await query.CountAsync(cancellationToken);

        // Sıralama — bilinmeyen alan fullName asc'e düşer
        query = (pagedQuery.SortBy?.ToLower()) switch
        {
            "title"    => pagedQuery.IsDescending ? query.OrderByDescending(e => e.Title)    : query.OrderBy(e => e.Title),
            "email"    => pagedQuery.IsDescending ? query.OrderByDescending(e => e.Email)    : query.OrderBy(e => e.Email),
            // Varsayılan: fullName asc
            _          => pagedQuery.IsDescending ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName)
        };

        var employees = await query
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = await MapToDtoListAsync(employees, cancellationToken);
        return new PagedResult<EmployeeDto>(dtos, pagedQuery.Page, pagedQuery.PageSize, totalCount);
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        return await MapToDtoAsync(employee, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Mutasyonlar
    // -----------------------------------------------------------------------

    public async Task<CreateEmployeeResult> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var (all, ids) = await ResolveScopeAsync(cancellationToken);

        // Atanacak yönetici kapsam içinde mi?
        if (request.ManagerId.HasValue)
        {
            if (!all && !ids.Contains(request.ManagerId.Value))
                throw new ForbiddenAppException("Atanacak yönetici kapsam dışında.");
        }

        var employee = new Employee(request.Title, request.FullName, request.Email, request.ManagerId);
        db.Set<Employee>().Add(employee);

        AccountCredentialDto? account = null;

        if (request.CreateAccount)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ConflictException("Hesap oluşturmak için e-posta zorunludur.");

            // E-posta benzersizliği kontrolü
            if (await userManager.FindByEmailAsync(request.Email) is not null)
                throw new ConflictException($"Bu e-posta adresi zaten kullanımda: {request.Email}");

            // Geçici parola üret
            var tempPassword = GenerateCompliantPassword();

            var result = await identityService.CreateUserAsync(
                request.Email,
                tempPassword,
                request.FullName ?? request.Title,
                request.Role!,
                cancellationToken);

            if (!result.Succeeded || result.UserId is null)
                throw new ConflictException($"Hesap oluşturulamadı: {string.Join("; ", result.Errors)}");

            employee.LinkAccount(result.UserId.Value);
            account = new AccountCredentialDto(request.Email, tempPassword);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Manager navigasyonunu yükle
        var saved = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == employee.Id, cancellationToken);

        var dto = await MapToDtoAsync(saved, cancellationToken);
        return new CreateEmployeeResult(dto, account);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        employee.UpdateDetails(request.Title, request.FullName, request.Email);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == id, cancellationToken);

        return await MapToDtoAsync(updated, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        // Astı olan düğüm silinemez (soft-delete sonrası da hiyerarşi bozulmamalı)
        var hasSubordinates = await db.Set<Employee>()
            .AnyAsync(e => e.ManagerId == id, cancellationToken);

        if (hasSubordinates)
            throw new ConflictException("Astı bulunan personel silinemez. Önce astların yöneticisini değiştirin.");

        // Fiziksel silme yerine soft-delete; global query filter sorgularda gizler.
        employee.MarkDeleted(clock.UtcNow);
        db.Set<Employee>().Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeDto> AssignManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default)
    {
        var (all, ids) = await ResolveScopeAsync(cancellationToken);

        if (!all && !ids.Contains(id))
            throw new ForbiddenAppException($"Personel (id: {id}) yönetim kapsamınızın dışında.");

        // Yeni yönetici de kapsam içinde olmalı
        if (managerId.HasValue && !all && !ids.Contains(managerId.Value))
            throw new ForbiddenAppException("Atanacak yönetici kapsam dışında.");

        var employee = await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        if (managerId.HasValue)
        {
            // Kendine atama döngüsünü engelle
            if (managerId.Value == id)
                throw new ConflictException("Personel kendi yöneticisi olarak atanamaz.");

            // Alt-ağaca atama döngüsünü engelle
            var subtree = await orgScopeService.GetSubtreeIdsAsync(id, cancellationToken);
            if (subtree.Contains(managerId.Value))
                throw new ConflictException("Bir personel kendi alt-ağacındaki birine bağlanamaz (döngü oluşur).");

            // Yöneticinin var olduğunu doğrula
            var managerExists = await db.Set<Employee>()
                .AnyAsync(e => e.Id == managerId.Value, cancellationToken);
            if (!managerExists)
                throw NotFoundException.For("Yönetici", managerId.Value);
        }

        employee.AssignManager(managerId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == id, cancellationToken);

        return await MapToDtoAsync(updated, cancellationToken);
    }

    public async Task<AccountCredentialDto> CreateAccountAsync(Guid id, CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        if (employee.ApplicationUserId.HasValue)
            throw new ConflictException("Personelin zaten bir hesabı var.");

        if (string.IsNullOrWhiteSpace(employee.Email))
            throw new ConflictException("Hesap oluşturmak için personelin e-posta adresi olmalıdır. Önce güncelleme yapın.");

        if (await userManager.FindByEmailAsync(employee.Email) is not null)
            throw new ConflictException($"Bu e-posta adresi zaten kullanımda: {employee.Email}");

        var tempPassword = GenerateCompliantPassword();

        var result = await identityService.CreateUserAsync(
            employee.Email,
            tempPassword,
            employee.FullName ?? employee.Title,
            request.Role,
            cancellationToken);

        if (!result.Succeeded || result.UserId is null)
            throw new ConflictException($"Hesap oluşturulamadı: {string.Join("; ", result.Errors)}");

        employee.LinkAccount(result.UserId.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccountCredentialDto(employee.Email, tempPassword);
    }

    public async Task<EmployeeDto> AssignRoleAsync(Guid id, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        if (!employee.ApplicationUserId.HasValue)
            throw new ConflictException("Personelin hesabı yok. Önce hesap oluşturun.");

        await identityService.SetRoleAsync(employee.ApplicationUserId.Value, request.Role, cancellationToken);

        return await MapToDtoAsync(employee, cancellationToken);
    }

    public async Task<AccountCredentialDto> ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        if (!employee.ApplicationUserId.HasValue)
            throw new ConflictException("Personelin hesabı yok.");

        var newPassword = await identityService.ResetPasswordAsync(employee.ApplicationUserId.Value, cancellationToken);

        var email = employee.Email
            ?? (await userManager.FindByIdAsync(employee.ApplicationUserId.Value.ToString()))?.Email
            ?? string.Empty;

        return new AccountCredentialDto(email, newPassword);
    }

    public async Task<EmployeeDto> UnlinkAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInScopeAsync(id, cancellationToken);

        var employee = await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Personel", id);

        if (!employee.ApplicationUserId.HasValue)
            throw new ConflictException("Personelin bağlı hesabı yok.");

        employee.UnlinkAccount();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await db.Set<Employee>()
            .AsNoTracking()
            .Include(e => e.Manager)
            .FirstAsync(e => e.Id == id, cancellationToken);

        return await MapToDtoAsync(updated, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Yardımcı metotlar
    // -----------------------------------------------------------------------

    private async Task<IReadOnlyList<EmployeeDto>> MapToDtoListAsync(
        IEnumerable<Employee> employees,
        CancellationToken cancellationToken)
    {
        var list = employees.ToList();

        var linkedUserIds = list
            .Where(e => e.ApplicationUserId.HasValue)
            .Select(e => e.ApplicationUserId!.Value)
            .ToHashSet();

        var roleMap = await BuildRoleMapAsync(linkedUserIds);

        return list.Select(e => ToDto(e, roleMap)).ToList();
    }

    private async Task<EmployeeDto> MapToDtoAsync(Employee employee, CancellationToken cancellationToken)
    {
        var roleMap = employee.ApplicationUserId.HasValue
            ? await BuildRoleMapAsync([employee.ApplicationUserId.Value])
            : new Dictionary<Guid, string?>();

        return ToDto(employee, roleMap);
    }

    private async Task<Dictionary<Guid, string?>> BuildRoleMapAsync(IEnumerable<Guid> userIds)
    {
        var map = new Dictionary<Guid, string?>();
        foreach (var userId in userIds)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                var roles = await userManager.GetRolesAsync(user);
                map[userId] = roles.FirstOrDefault();
            }
        }
        return map;
    }

    private static EmployeeDto ToDto(Employee e, Dictionary<Guid, string?> roleMap)
    {
        var managerName = e.Manager is null
            ? null
            : e.Manager.FullName ?? e.Manager.Title;

        var role = e.ApplicationUserId.HasValue
            ? roleMap.GetValueOrDefault(e.ApplicationUserId.Value)
            : null;

        return new EmployeeDto(
            e.Id,
            e.FullName,
            e.Title,
            e.Email,
            e.ManagerId,
            managerName,
            e.ApplicationUserId.HasValue,
            role);
    }

    /// <summary>≥12 karakter, en az 1 büyük/küçük/rakam/simge içeren politika-uyumlu parola üretir.</summary>
    private static string GenerateCompliantPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        var password = new char[12];
        password[0] = upper[bytes[0] % upper.Length];
        password[1] = lower[bytes[1] % lower.Length];
        password[2] = digits[bytes[2] % digits.Length];
        password[3] = special[bytes[3] % special.Length];

        for (int i = 4; i < 12; i++)
            password[i] = all[bytes[i] % all.Length];

        // Fisher-Yates karıştır
        rng.GetBytes(bytes);
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = bytes[i % bytes.Length] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
