using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Oypa.Crm.Contracts.Common;

namespace Oypa.Crm.Api.Filters;

/// <summary>Action argümanları için kayıtlı FluentValidation validator'larını çalıştırır.</summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (result.IsValid) continue;

            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            // Alan-bazlı hatalar: PropertyName camelCase'e çevrilir (istemci RHF alanlarına bağlar).
            var fieldErrors = result.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            context.Result = new BadRequestObjectResult(
                ApiResponse.Fail("Doğrulama hatası.", errors, fieldErrors));
            return;
        }

        await next();
    }

    /// <summary>"Title" → "title", "TenderDate" → "tenderDate" (boş/tek karakter güvenli).</summary>
    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
            return propertyName;
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
