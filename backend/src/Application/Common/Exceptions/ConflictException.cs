namespace Oypa.Crm.Application.Common.Exceptions;

/// <summary>İş kuralı çakışması (örn. tekrar eden kayıt) durumunda fırlatılır (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);
