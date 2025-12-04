using System.Data;
using System.Text.Json;
using ExcelDataReader;

namespace MultiAdsConnect.Services
{
    public class SpreadsheetService : ISpreadsheetService
    {
        private readonly IGeminiService _geminiService;

        public SpreadsheetService(IGeminiService geminiService)
        {
            _geminiService = geminiService;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public async Task<(bool Success, string Message, object? GeminiResponse)> ProcessAndSendAsync(
            Stream fileStream, string fileName, CancellationToken ct = default)
        {
            try
            {
                using var reader = ExcelReaderFactory.CreateReader(fileStream);

                var conf = new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                };

                var ds = reader.AsDataSet(conf);

                if (ds.Tables.Count == 0)
                    return (false, "Arquivo sem planilhas.", null);

                var table = ds.Tables[0];

                var list = DataTableToDictionaryList(table);

                if (list.Count == 0)
                    return (false, "Planilha sem linhas válidas.", null);

                var json = JsonSerializer.Serialize(list);

                var respostaGemini = await _geminiService.AnalisarRelatorioAdsAsync(json);

                object? parsed = null;
                try { parsed = JsonSerializer.Deserialize<object>(respostaGemini); }
                catch { parsed = respostaGemini; }

                return (true, "Enviado com sucesso.", parsed);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        private List<Dictionary<string, object?>> DataTableToDictionaryList(DataTable table)
        {
            var list = new List<Dictionary<string, object?>>();

            foreach (DataRow row in table.Rows)
            {
                if (table.Columns.Cast<DataColumn>().All(c =>
                    string.IsNullOrWhiteSpace(row[c]?.ToString())))
                    continue;

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (DataColumn col in table.Columns)
                {
                    var key = (col.ColumnName ?? $"Column{col.Ordinal}").Trim();
                    var val = row[col];
                    dict[key] = val == DBNull.Value ? null : val;
                }

                list.Add(dict);
            }

            return list;
        }
    }
}
