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
            context.Result = new BadRequestObjectResult(ApiResponse.Fail("Doğrulama hatası.", errors));
            return;
        }

        await next();
    }
}
