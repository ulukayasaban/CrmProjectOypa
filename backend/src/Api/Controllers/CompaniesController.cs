using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.Companies;
using Oypa.Crm.Application.Features.Contacts;
using Oypa.Crm.Application.Features.Meetings;
using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Contracts.Contacts;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Enums;


namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public sealed class CompaniesController(
    ICompanyService companyService,
    IContactService contactService,
    IMeetingService meetingService) : ControllerBase
{
    [HttpGet("leads")]
    [EnableRateLimiting(RateLimitingExtensions.Search)]
    public async Task<IActionResult> GetLeads([FromQuery] LeadStatus? status, CancellationToken cancellationToken)
    {
        var data = await companyService.GetLeadsAsync(status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CompanyDto>>.Ok(data));
    }

    /// <summary>
    /// Lead firmaları sayfalama + arama + sıralama ile listeler (Takvim/Sidebar etkilenmez).
    /// sortBy: title | city | createdAt (varsayılan: createdAt desc)
    /// </summary>
    [HttpGet("leads/paged")]
    [EnableRateLimiting(RateLimitingExtensions.Search)]
    public async Task<IActionResult> GetLeadsPaged(
        [FromQuery] LeadStatus? status,
        [FromQuery] PagedQuery query,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var data = await companyService.GetLeadsPagedAsync(status, query, categoryId, cancellationToken);
        return Ok(ApiResponse<PagedResult<CompanyDto>>.Ok(data));
    }

    [HttpGet("customers")]
    [EnableRateLimiting(RateLimitingExtensions.Search)]
    public async Task<IActionResult> GetCustomers([FromQuery] CustomerStatus? status, CancellationToken cancellationToken)
    {
        var data = await companyService.GetCustomersAsync(status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CompanyDto>>.Ok(data));
    }

    /// <summary>
    /// Müşteri firmaları sayfalama + arama + sıralama ile listeler (Takvim/Sidebar etkilenmez).
    /// sortBy: title | city | createdAt (varsayılan: createdAt desc)
    /// </summary>
    [HttpGet("customers/paged")]
    [EnableRateLimiting(RateLimitingExtensions.Search)]
    public async Task<IActionResult> GetCustomersPaged(
        [FromQuery] CustomerStatus? status,
        [FromQuery] PagedQuery query,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var data = await companyService.GetCustomersPagedAsync(status, query, categoryId, cancellationToken);
        return Ok(ApiResponse<PagedResult<CompanyDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await companyService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<CompanyDto>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var data = await companyService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CompanyDto>.Ok(data, "Firma oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var data = await companyService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CompanyDto>.Ok(data, "Firma güncellendi."));
    }

    [HttpPatch("{id:guid}/lead-status")]
    public async Task<IActionResult> SetLeadStatus(Guid id, SetLeadStatusRequest request, CancellationToken cancellationToken)
    {
        await companyService.SetLeadStatusAsync(id, request.Status, cancellationToken);
        return Ok(ApiResponse.Ok("Durum güncellendi."));
    }

    [HttpPatch("{id:guid}/customer-status")]
    public async Task<IActionResult> SetCustomerStatus(Guid id, SetCustomerStatusRequest request, CancellationToken cancellationToken)
    {
        await companyService.SetCustomerStatusAsync(id, request.Status, cancellationToken);
        return Ok(ApiResponse.Ok("Müşteri durumu güncellendi."));
    }

    /// <summary>
    /// Lead'i müşteriye dönüştürür.
    /// Body opsiyoneldir; verilirse salesRepId (null=Havuz), serviceSector, isNewCustomer uygulanır.
    /// </summary>
    [HttpPost("{id:guid}/convert")]
    public async Task<IActionResult> ConvertToCustomer(Guid id, [FromBody] ConvertToCustomerRequest? request, CancellationToken cancellationToken)
    {
        var data = await companyService.ConvertToCustomerAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CompanyDto>.Ok(data, "Firma müşteriye dönüştürüldü."));
    }

    [HttpPatch("{id:guid}/assign-rep")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> AssignSalesRep(Guid id, AssignSalesRepRequest request, CancellationToken cancellationToken)
    {
        await companyService.AssignSalesRepAsync(id, request.SalesRepId, cancellationToken);
        return Ok(ApiResponse.Ok("Temsilci güncellendi."));
    }

    /// <summary>Firmaya atanacak kategorileri toptan günceller.</summary>
    [HttpPut("{id:guid}/categories")]
    public async Task<IActionResult> SetCategories(Guid id, SetCompanyCategoriesRequest request, CancellationToken cancellationToken)
    {
        var data = await companyService.SetCategoriesAsync(id, request.CategoryIds, cancellationToken);
        return Ok(ApiResponse<CompanyDto>.Ok(data, "Kategoriler güncellendi."));
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> GetContacts(Guid id, CancellationToken cancellationToken)
    {
        var data = await contactService.GetByCompanyAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ContactDto>>.Ok(data));
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> AddContact(Guid id, CreateContactRequest request, CancellationToken cancellationToken)
    {
        var data = await contactService.AddAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ContactDto>.Ok(data, "İlgili kişi eklendi."));
    }

    /// <summary>Belirtilen ilgili kişiyi Id ile döndürür.</summary>
    [HttpGet("contacts/{contactId:guid}")]
    public async Task<IActionResult> GetContactById(Guid contactId, CancellationToken cancellationToken)
    {
        var data = await contactService.GetByIdAsync(contactId, cancellationToken);
        return Ok(ApiResponse<ContactDto>.Ok(data));
    }

    /// <summary>İlgili kişiyi günceller.</summary>
    [HttpPut("contacts/{contactId:guid}")]
    public async Task<IActionResult> UpdateContact(Guid contactId, UpdateContactRequest request, CancellationToken cancellationToken)
    {
        var data = await contactService.UpdateAsync(contactId, request, cancellationToken);
        return Ok(ApiResponse<ContactDto>.Ok(data, "İlgili kişi güncellendi."));
    }

    /// <summary>İlgili kişiyi siler.</summary>
    [HttpDelete("contacts/{contactId:guid}")]
    public async Task<IActionResult> DeleteContact(Guid contactId, CancellationToken cancellationToken)
    {
        await contactService.DeleteAsync(contactId, cancellationToken);
        return Ok(ApiResponse.Ok("İlgili kişi silindi."));
    }

    [HttpGet("{id:guid}/meetings")]
    public async Task<IActionResult> GetMeetings(Guid id, CancellationToken cancellationToken)
    {
        var data = await meetingService.GetByCompanyAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MeetingDto>>.Ok(data));
    }
}
