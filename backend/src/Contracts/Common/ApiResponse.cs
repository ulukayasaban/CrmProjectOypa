namespace Oypa.Crm.Contracts.Common;

/// <summary>Veri taşıyan standart API yanıt zarfı.</summary>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Alan-bazlı doğrulama hataları (camelCase alan adı → mesajlar). İstemci form alanlarına bağlar.</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "İşlem başarılı") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(
        string message,
        IReadOnlyList<string>? errors = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? [], FieldErrors = fieldErrors };
}

/// <summary>Veri taşımayan (yalnızca mesaj) standart API yanıt zarfı.</summary>
public sealed record ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Alan-bazlı doğrulama hataları (camelCase alan adı → mesajlar).</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }

    public static ApiResponse Ok(string message = "İşlem başarılı") =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(
        string message,
        IReadOnlyList<string>? errors = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? [], FieldErrors = fieldErrors };
}
