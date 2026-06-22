using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Application.Features.Employees;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Employees;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    /// <summary>Tüm org ağacı (org şeması için).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await employeeService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EmployeeDto>>.Ok(data));
    }

    /// <summary>Çağıranın yönetim kapsamındaki personel listesi.</summary>
    [HttpGet("managed")]
    public async Task<IActionResult> GetManaged(CancellationToken cancellationToken)
    {
        var data = await employeeService.GetManagedAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EmployeeDto>>.Ok(data));
    }

    /// <summary>Yeni personel oluşturur. CreateAccount == true ise geçici kimlik bilgilerini döndürür.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await employeeService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreateEmployeeResult>.Ok(result, "Personel oluşturuldu."));
    }

    /// <summary>Personel ünvan/ad/e-posta güncelleme.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var data = await employeeService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDto>.Ok(data, "Personel güncellendi."));
    }

    /// <summary>Personeli siler (astı varsa 409 döner).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await employeeService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Personel silindi."));
    }

    /// <summary>Yönetici atar veya kaldırır (null = kök düğüm).</summary>
    [HttpPut("{id:guid}/manager")]
    public async Task<IActionResult> AssignManager(Guid id, AssignManagerRequest request, CancellationToken cancellationToken)
    {
        var data = await employeeService.AssignManagerAsync(id, request.ManagerId, cancellationToken);
        return Ok(ApiResponse<EmployeeDto>.Ok(data, "Yönetici atandı."));
    }

    /// <summary>Hesapsız personele hesap ve rol oluşturur; geçici kimlik bilgilerini döndürür.</summary>
    [HttpPost("{id:guid}/account")]
    public async Task<IActionResult> CreateAccount(Guid id, CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var data = await employeeService.CreateAccountAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<AccountCredentialDto>.Ok(data, "Hesap oluşturuldu."));
    }

    /// <summary>Mevcut hesaba rol atar.</summary>
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var data = await employeeService.AssignRoleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDto>.Ok(data, "Rol atandı."));
    }

    /// <summary>Parolayı sıfırlar; yeni geçici kimlik bilgilerini döndürür.</summary>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var data = await employeeService.ResetPasswordAsync(id, cancellationToken);
        return Ok(ApiResponse<AccountCredentialDto>.Ok(data, "Parola sıfırlandı."));
    }

    /// <summary>Kimlik hesabı bağlantısını personelden kaldırır.</summary>
    [HttpDelete("{id:guid}/account")]
    public async Task<IActionResult> UnlinkAccount(Guid id, CancellationToken cancellationToken)
    {
        var data = await employeeService.UnlinkAccountAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeDto>.Ok(data, "Hesap bağlantısı kaldırıldı."));
    }
}
