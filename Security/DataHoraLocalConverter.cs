using System.Text.Json;
using System.Text.Json.Serialization;

namespace API_PRODUCAO.Security
{
    /// <summary>
    /// Padroniza datas em HORA LOCAL do servidor (mesmo padrão de DateTime.Now usado nas OPs).
    /// Os clientes web enviam datas em UTC ("...Z", via toISOString); sem esta conversão elas eram
    /// gravadas 3h adiantadas. Na leitura, qualquer data com fuso é convertida para hora local.
    /// Na escrita, a data sai sem fuso, e o cliente a interpreta como hora local.
    /// </summary>
    public class DataHoraLocalConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var valor = reader.GetDateTime();
            return valor.Kind switch
            {
                DateTimeKind.Utc   => DateTime.SpecifyKind(valor.ToLocalTime(), DateTimeKind.Unspecified),
                DateTimeKind.Local => DateTime.SpecifyKind(valor, DateTimeKind.Unspecified),
                _                  => valor,
            };
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
