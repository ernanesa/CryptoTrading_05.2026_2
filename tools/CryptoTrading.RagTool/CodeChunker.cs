using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CryptoTrading.RagTool;

public static class CodeChunker
{
    public static List<MarkdownChunk> ParseFile(string filePath, string repoRoot)
    {
        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var formattedChunks = new List<MarkdownChunk>();
        var indexedAt = DateTime.UtcNow.ToString("o");

        const int chunkSize = 60;
        const int overlap = 20;

        if (lines.Length <= chunkSize)
        {
            var chunkContent = $"Arquivo de Código: {relativePath}\nLinhas: 1-{lines.Length}\n\n{content}";
            formattedChunks.Add(new MarkdownChunk(
                Content: chunkContent,
                SourceFile: relativePath,
                Title: Path.GetFileName(filePath),
                Section: "Completo",
                SourceType: "code",
                CreatedAt: "2026-05-21",
                IndexedAt: indexedAt
            ));
        }
        else
        {
            int start = 0;
            while (start < lines.Length)
            {
                int end = Math.Min(start + chunkSize, lines.Length);
                var chunkLines = new List<string>();
                for (int i = start; i < end; i++)
                {
                    chunkLines.Add(lines[i]);
                }

                var codeText = string.Join("\n", chunkLines);
                var chunkContent = $"Arquivo de Código: {relativePath}\nLinhas: {start + 1}-{end}\n\n{codeText}";

                formattedChunks.Add(new MarkdownChunk(
                    Content: chunkContent,
                    SourceFile: relativePath,
                    Title: Path.GetFileName(filePath),
                    Section: $"Linhas {start + 1}-{end}",
                    SourceType: "code",
                    CreatedAt: "2026-05-21",
                    IndexedAt: indexedAt
                ));

                start += (chunkSize - overlap);
            }
        }

        return formattedChunks;
    }
}
