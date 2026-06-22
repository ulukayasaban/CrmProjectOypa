namespace Oypa.Crm.Application.Common.Exceptions;

/// <summary>Kimlik doğrulama başarısız olduğunda fırlatılır (HTTP 401).</summary>
public sealed class UnauthorizedAppException(string message) : Exception(message);
