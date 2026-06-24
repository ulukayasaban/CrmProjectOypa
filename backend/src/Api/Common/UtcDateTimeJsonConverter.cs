using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oypa.Crm.Api.Common;

/// <summary>
/// Tüm <see cref="DateTime"/> değerlerini UTC olarak serileştirir (ISO-8601 + 'Z').
/// Sebep: Entity'lerdeki *Utc alanları UTC olarak saklanır, ancak EF Core SQL Server'dan
/// bunları <see cref="DateTimeKind.Unspecified"/> olarak okur. System.Text.Json bu durumda
/// 'Z'/offset olmadan yazar ("2026-06-23T12:16:55") ve tarayıcı bu değeri YEREL kabul edip
/// saatleri kaydırır. Bu converter değeri UTC olarak işaretleyip 'Z' ekleyerek istemcinin
/// doğru yerel saate dönüştürmesini sağlar. Nullable (DateTime?) alanlar için de otomatik kullanılır.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("O"));
}
