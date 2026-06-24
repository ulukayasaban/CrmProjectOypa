using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Models;
using Oypa.Crm.Infrastructure.Persistence;

namespace Oypa.Crm.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db) : IIdentityService
{
    public async Task<AuthUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        // Hesap kilitli mi? Kilitliyse hemen 401 fırlat; yanlış parola bilgisi verme.
        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAppException("Hesap geçici olarak kilitlendi.");

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Başarısız girişi kaydet; bu çağrı gerektiğinde kilidi otomatik devreye alır.
            await userManager.AccessFailedAsync(user);

            // Kilitlenme eşiğine ulaşıldıysa tutarlı mesaj döndür.
            if (await userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAppException("Hesap geçici olarak kilitlendi.");

            return null;
        }

        // Başarılı giriş: başarısız sayacı sıfırla.
        await userManager.ResetAccessFailedCountAsync(user);

        return await ToInfoAsync(user);
    }

    public async Task<AuthUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToInfoAsync(user);
    }

    public async Task<CreateUserResult> CreateUserAsync(
        string email, string password, string fullName, string role, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return new CreateUserResult(false, null, ["Bu e-posta adresi zaten kayıtlı."]);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new CreateUserResult(false, null, result.Errors.Select(e => e.Description).ToList());

        if (!string.IsNullOrWhiteSpace(role))
            await userManager.AddToRoleAsync(user, role);

        return new CreateUserResult(true, user.Id, []);
    }

    public async Task SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"Kullanıcı bulunamadı (id: {userId}).");

        // Geçerli rol kümesini doğrula
        if (role is not ("Admin" or "Sales"))
            throw new ConflictException($"Geçersiz rol: {role}. Yalnızca 'Admin' veya 'Sales' atanabilir.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Any())
            await userManager.RemoveFromRolesAsync(user, currentRoles);

        await userManager.AddToRoleAsync(user, role);
    }

    public async Task<string> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"Kullanıcı bulunamadı (id: {userId}).");

        var newPassword = GenerateCompliantPassword();

        await userManager.RemovePasswordAsync(user);
        var result = await userManager.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded)
            throw new ConflictException(
                $"Parola sıfırlanamadı: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return newPassword;
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"Kullanıcı bulunamadı (id: {userId}).");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? (true, [])
            : (false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<(string? Email, string? Token)> GeneratePasswordResetTokenAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Kullanıcı bulunamazsa null çifti döner; çağıran katman varlık sızdırmaz.
        if (user is null)
            return (null, null);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return (user.Email, token);
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> ResetPasswordWithTokenAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Kullanıcı bulunamazsa da genel hata ver — e-posta varlığını sızdırma.
        if (user is null)
            return (false, ["Parola sıfırlama başarısız. Bağlantı geçersiz veya süresi dolmuş olabilir."]);

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? (true, [])
            : (false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<AuthUserInfo> UpdateProfileAsync(
        Guid userId, string fullName, string? phone, string? position, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"Kullanıcı bulunamadı (id: {userId}).");

        user.FullName = fullName;
        user.PhoneNumber = phone;
        user.Position = position;

        // UpdateAsync, EF üzerinden değişiklikleri kaydeder.
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ConflictException(
                $"Profil güncellenemedi: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return await ToInfoAsync(user);
    }

    /// <summary>
    /// En az 12 karakter, en az 1 büyük harf, 1 küçük harf, 1 rakam, 1 özel karakter içeren
    /// rastgele parola üretir.
    /// </summary>
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

        // Zorunlu karakterleri garantile
        var password = new char[12];
        password[0] = upper[bytes[0] % upper.Length];
        password[1] = lower[bytes[1] % lower.Length];
        password[2] = digits[bytes[2] % digits.Length];
        password[3] = special[bytes[3] % special.Length];

        // Geri kalanı all havuzundan doldur
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

    public async Task<IReadOnlyList<AuthUserInfo>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        // Tüm kullanıcıları bellekte çek; her biri için rol sorgula.
        // Kullanıcı sayısı genelde az olduğundan N+1 burada kabul edilebilir.
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        var result = new List<AuthUserInfo>(users.Count);
        foreach (var user in users)
            result.Add(await ToInfoAsync(user));

        return result;
    }

    public async Task DeleteUserAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        // Kendini silme girişimini engelle — güvenlik kuralı.
        if (userId == currentUserId)
            throw new ForbiddenAppException("Kendi hesabınızı silemezsiniz.");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"Kullanıcı bulunamadı (id: {userId}).");

        // Bağlı personel kaydı varsa ApplicationUserId null'a düşür (Employee kaydını koru).
        var linkedEmployee = await db.Employees
            .FirstOrDefaultAsync(e => e.ApplicationUserId == userId, cancellationToken);

        if (linkedEmployee is not null)
        {
            linkedEmployee.UnlinkAccount();
            // db.SaveChangesAsync, kullanıcı silinmeden önce çağrılmaz;
            // Identity UserManager.DeleteAsync kendi içinde SaveChanges yapar.
            // EF change tracker bu değişikliği takip eder; sonraki SaveChanges'da yazılır.
            await db.SaveChangesAsync(cancellationToken);
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new ConflictException(
                $"Kullanıcı silinemedi: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }

    private async Task<AuthUserInfo> ToInfoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthUserInfo(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Position,
            user.PhoneNumber,
            [.. roles]);
    }
}
