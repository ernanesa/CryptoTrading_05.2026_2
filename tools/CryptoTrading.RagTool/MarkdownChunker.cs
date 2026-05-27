using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CryptoTrading.RagTool;

public record MarkdownChunk(
    string Content,
    string SourceFile,
    string Title,
    string Section,
    string SourceType,
    string CreatedAt,
    string IndexedAt
);

public static class MarkdownChunker
{
    public static List<MarkdownChunk> ParseFile(string filePath, string repoRoot)
    {
        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var chunks = new List<RawChunk>();

        var currentH1 = "Sem título";
        var currentH2 = "";
        var currentSection = "";
        var currentText = new List<string>();

        var titleMatch = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
        if (titleMatch.Success)
        {
            currentH1 = titleMatch.Groups[1].Value.Trim();
        }

        foreach (var line in lines)
        {
            if (line.StartsWith("# "))
            {
                if (currentText.Count > 0)
                {
                    chunks.Add(new RawChunk(string.Join("\n", currentText), currentH1, currentH2, currentSection));
                }
                currentH1 = line[2..].Trim();
                currentH2 = "";
                currentSection = currentH1;
                currentText = new List<string> { line };
            }
            else if (line.StartsWith("## "))
            {
                if (currentText.Count > 0)
                {
                    chunks.Add(new RawChunk(string.Join("\n", currentText), currentH1, currentH2, currentSection));
                }
                currentH2 = line[3..].Trim();
                currentSection = $"{currentH1} > {currentH2}";
                currentText = new List<string> { line };
            }
            else if (line.StartsWith("### "))
            {
                if (currentText.Count > 0)
                {
                    chunks.Add(new RawChunk(string.Join("\n", currentText), currentH1, currentH2, currentSection));
                }
                var h3Val = line[4..].Trim();
                currentSection = $"{currentH1} > {currentH2} > {h3Val}";
                currentText = new List<string> { line };
            }
            else
            {
                currentText.Add(line);
            }
        }

        if (currentText.Count > 0)
        {
            chunks.Add(new RawChunk(string.Join("\n", currentText), currentH1, currentH2, currentSection));
        }

        var formattedChunks = new List<MarkdownChunk>();
        var indexedAt = DateTime.UtcNow.ToString("o");

        foreach (var chunk in chunks)
        {
            var text = chunk.Text.Trim();
            if (text.Length < 20) continue;

            var sourceType = "doc";
            if (relativePath.Contains("adr-", StringComparison.OrdinalIgnoreCase))
            {
                sourceType = "decision";
            }
            else if (relativePath.Contains("checklist", StringComparison.OrdinalIgnoreCase) || 
                     relativePath.Contains("task", StringComparison.OrdinalIgnoreCase) ||
                     relativePath.Contains("plan", StringComparison.OrdinalIgnoreCase))
            {
                sourceType = "task";
            }

            var chunkContent = $"Documento: {relativePath}\nSeção: {chunk.Section}\n\n{text}";

            formattedChunks.Add(new MarkdownChunk(
                Content: chunkContent,
                SourceFile: relativePath,
                Title: chunk.H1,
                Section: chunk.Section ?? chunk.H1,
                SourceType: sourceType,
                CreatedAt: DateTime.UtcNow.ToString("yyyy-MM-dd"),
                IndexedAt: indexedAt
            ));
        }

        return formattedChunks;
    }

    private record RawChunk(string Text, string H1, string H2, string Section);
}
