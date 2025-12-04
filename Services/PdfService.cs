using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;

namespace MultiAdsConnect.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateReportPdf(string reportText)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);

                    page.Header()
                        .Text("📊 Relatório de Análise de Campanhas")
                        .FontSize(20)
                        .Bold()
                        .FontColor("#252525")
                        .AlignCenter();

                    page.Content()
                        .Column(col =>
                        {
                            foreach (var block in ParseMarkdown(reportText))
                            {
                                if (block.Type == BlockType.Heading)
                                {
                                    col.Item()
                                        .PaddingTop(15)
                                        .PaddingBottom(8)
                                        .Text(block.Content)
                                        .FontSize(14)
                                        .SemiBold()
                                        .FontColor("#000");
                                }
                                else
                                {
                                    col.Item()
                                    .PaddingBottom(10)
                                    .Text(t =>
                                    {
                                        foreach (var fragment in ParseInlineFormatting(block.Content))
                                        {
                                            var style = t.Span(fragment.Text).FontSize(11).FontColor("#333");

                                            if (fragment.Bold)
                                                style.SemiBold();
                                        }
                                    });
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Gerado automaticamente — MultiAdsConnect")
                        .FontSize(9)
                        .FontColor("#777");
                });
            });

            return document.GeneratePdf();
        }

        // -----------------------
        // MARKDOWN → BLOCO
        // -----------------------
        private enum BlockType { Paragraph, Heading }

        private record Block(BlockType Type, string Content);

        private static IEnumerable<Block> ParseMarkdown(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            var lines = text.Replace("\r", "").Split("\n");

            var blocks = new List<Block>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed.StartsWith("### "))
                {
                    blocks.Add(new Block(BlockType.Heading, trimmed[4..].Trim()));
                }
                else
                {
                    blocks.Add(new Block(BlockType.Paragraph, trimmed));
                }
            }

            return blocks;
        }

        // -----------------------
        // INLINE: **bold**
        // -----------------------
        private record TextFragment(string Text, bool Bold);

        private static IEnumerable<TextFragment> ParseInlineFormatting(string text)
        {
            var list = new List<TextFragment>();

            var regex = new Regex(@"\*\*(.*?)\*\*"); // captura **bold**
            var matches = regex.Matches(text);

            int lastIndex = 0;

            foreach (Match match in matches)
            {
                // texto antes do bold
                if (match.Index > lastIndex)
                {
                    var before = text[lastIndex..match.Index];
                    list.Add(new TextFragment(before, false));
                }

                // texto bold
                var boldText = match.Groups[1].Value;
                list.Add(new TextFragment(boldText, true));

                lastIndex = match.Index + match.Length;
            }

            // resto da linha
            if (lastIndex < text.Length)
            {
                var after = text[lastIndex..];
                list.Add(new TextFragment(after, false));
            }

            return list;
        }
    }
}
