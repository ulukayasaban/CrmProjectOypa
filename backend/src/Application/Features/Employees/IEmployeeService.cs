using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Employees;

namespace Oypa.Crm.Application.Features.Employees;

public interface IEmployeeService
{
    /// <summary>Tüm org ağacı — org şeması için.</summary>
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Çağıranın yönetim kapsamındaki personel listesi.</summary>
    Task<IReadOnlyList<EmployeeDto>> GetManagedAsync(CancellationToken cancellationToken = default);

    /// <summary>Çağıranın yönetim kapsamındaki personeli sayfalama + arama + sıralama ile listeler.</summary>
    Task<PagedResult<EmployeeDto>> GetManagedPagedAsync(PagedQuery query, CancellationToken cancellationToken = default);

    /// <summary>Tek personel kaydı — kapsam kontrolü dahil.</summary>
    Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Yeni personel oluşturur; CreateAccount == true ise hesap + rol + geçici parola oluşturur.</summary>
    Task<CreateEmployeeResult> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ünvan/ad/e-posta günceller.</summary>
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Personeli siler. Astı varsa ConflictException fırlatır.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Yönetici atar veya kaldırır (managerId null = kök düğüm). Döngü engellenir.</summary>
    Task<EmployeeDto> AssignManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default);

    /// <summary>Hesapsız personele e-postası üzerinden hesap ve rol oluşturur.</summary>
    Task<AccountCredentialDto> CreateAccountAsync(Guid id, CreateAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>Mevcut hesaba yeni rol atar.</summary>
    Task<EmployeeDto> AssignRoleAsync(Guid id, AssignRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Parolayı sıfırlar; yeni geçici parolayı döndürür.</summary>
    Task<AccountCredentialDto> ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kimlik hesabı bağlantısını personelden kaldırır; ApplicationUser kaydına dokunulmaz.</summary>
    Task<EmployeeDto> UnlinkAccountAsync(Guid id, CancellationToken cancellationToken = default);
}
