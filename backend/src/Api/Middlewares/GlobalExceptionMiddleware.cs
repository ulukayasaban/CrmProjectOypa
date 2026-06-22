using System.Net;
using FluentValidation;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Contracts.Common;

namespace Oypa.Crm.Api.Middlewares;

/// <summary>Tüm beklenmeyen ve bilinen exception'ları standart <see cref="ApiResponse"/>'a çevirir.</summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, message, errors) = ex switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, "Doğrulama hatası.",
                ve.Errors.Select(e => e.ErrorMessage).ToList()),
            NotFoundException nf => (HttpStatusCode.NotFound, nf.Message, new List<string>()),
            ConflictException cf => (HttpStatusCode.Conflict, cf.Message, new List<string>()),
            UnauthorizedAppException ua => (HttpStatusCode.Unauthorized, ua.Message, new List<string>()),
            ForbiddenAppException fa => (HttpStatusCode.Forbidden, fa.Message, new List<string>()),
            _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.", new List<string>())
        };

        if (status == HttpStatusCode.InternalServerError)
            logger.LogError(ex, "İşlenmemiş hata: {Message}", ex.Message);

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message, errors));
    }
}
