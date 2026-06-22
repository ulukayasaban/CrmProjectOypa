using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

public sealed class ApiEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.SeedAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        payload!.Data!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        return payload.Data.AccessToken;
    }

    [Fact]
    public async Task Login_AdminCredentials_Returns200WithAccessToken()
    {
        var token = await LoginAndGetTokenAsync();
        token.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetDashboard_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/dashboard");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSalesReps_WithToken_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/salesreps");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<SalesRepDto>>>(JsonOptions);
        payload!.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task CompanyLifecycle_CreateListConvert_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create lead -> 201
        var createResponse = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Integration A.Ş.", Sector.Energy, "0212", "i@a.com", "Adres"),
            JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions);
        var companyId = created!.Data!.Id;
        created.Data.Type.ShouldBe(CompanyType.Lead);

        // Leads list contains the new lead
        var leadsResponse = await _client.GetAsync("/api/companies/leads");
        leadsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var leads = await leadsResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyDto>>>(JsonOptions);
        leads!.Data!.ShouldContain(c => c.Id == companyId);

        // Convert -> 200, type = Customer
        var convertResponse = await _client.PostAsync($"/api/companies/{companyId}/convert", null);
        convertResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var converted = await convertResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions);
        converted!.Data!.Type.ShouldBe(CompanyType.Customer);
    }
}
