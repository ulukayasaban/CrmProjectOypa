namespace Oypa.Crm.Application.Common.Exceptions;

/// <summary>İstenen kayıt bulunamadığında fırlatılır (HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For(string entity, object key) =>
        new($"{entity} bulunamadı (anahtar: {key}).");
}
