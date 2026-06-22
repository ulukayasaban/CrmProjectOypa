namespace Oypa.Crm.Application.Common.Exceptions;

/// <summary>Yetki kapsamı dışındaki kaynaklara erişim girişiminde fırlatılır (HTTP 403).</summary>
public sealed class ForbiddenAppException(string message) : Exception(message);
