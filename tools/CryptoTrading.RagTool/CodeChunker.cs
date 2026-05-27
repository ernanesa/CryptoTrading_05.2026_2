using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CryptoTrading.RagTool;

public static class CodeChunker
{
    public static List<MarkdownChunk> ParseFile(string filePath, string repoRoot)
    {
        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        var formattedChunks = new List<MarkdownChunk>();
        var indexedAt = DateTime.UtcNow.ToString("o");
        var fileName = Path.GetFileName(filePath);
        
        if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            var matches = Regex.Matches(content, @"(public|internal|private|protected)?\s*(static|sealed|abstract)?\s*(class|interface|record|struct)\s+(\w+)[\s\S]*?(?=(public|internal|private|protected)?\s*(static|sealed|abstract)?\s*(class|interface|record|struct)\s+(\w+)|\z)", RegexOptions.Multiline);
            
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    string matchText = m.Value.Trim();
                    if (matchText.Length < 20) continue;
                    
                    string sectionName = $"{m.Groups[3].Value} {m.Groups[4].Value}";
                    var chunkContent = $"Arquivo: {relativePath}\nTipo: {sectionName}\n\n{matchText}";
                    
                    formattedChunks.Add(new MarkdownChunk(
                        Content: chunkContent,
                        SourceFile: relativePath,
                        Title: fileName,
                        Section: sectionName,
                        SourceType: "code",
                        CreatedAt: DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        IndexedAt: indexedAt
                    ));
                }
            }
            else
            {
                formattedChunks.Add(FallbackChunk(content, relativePath, fileName, indexedAt));
            }
        }
        else if (filePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
        {
            var matches = Regex.Matches(content, @"(export\s+)?(default\s+)?(function|class|const|interface|type)\s+(\w+)[\s\S]*?(?=(export\s+)?(default\s+)?(function|class|const|interface|type)\s+(\w+)|\z)", RegexOptions.Multiline);
            
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    string matchText = m.Value.Trim();
                    if (matchText.Length < 20) continue;
                    
                    string sectionName = $"{m.Groups[3].Value} {m.Groups[4].Value}";
                    var chunkContent = $"Arquivo: {relativePath}\nComponente: {sectionName}\n\n{matchText}";
                    
                    formattedChunks.Add(new MarkdownChunk(
                        Content: chunkContent,
                        SourceFile: relativePath,
                        Title: fileName,
                        Section: sectionName,
                        SourceType: "code",
                        CreatedAt: DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        IndexedAt: indexedAt
                    ));
                }
            }
            else
            {
                formattedChunks.Add(FallbackChunk(content, relativePath, fileName, indexedAt));
            }
        }
        else
        {
            formattedChunks.Add(FallbackChunk(content, relativePath, fileName, indexedAt));
        }

        return formattedChunks;
    }

    private static MarkdownChunk FallbackChunk(string content, string relativePath, string fileName, string indexedAt)
    {
        return new MarkdownChunk(
            Content: $"Arquivo: {relativePath}\n\n{content}",
            SourceFile: relativePath,
            Title: fileName,
            Section: "Completo",
            SourceType: "code",
            CreatedAt: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            IndexedAt: indexedAt
        );
    }
}
