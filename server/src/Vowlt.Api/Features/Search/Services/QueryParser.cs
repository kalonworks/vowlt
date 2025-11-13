using System.Text;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// Parses user search queries into ParadeDB query syntax
/// Supports: AND/OR operators, phrase search ("exact phrase"), prefix matching (alex*)
/// </summary>
public static class QueryParser
{
    /// <summary>
    /// Parse user query into ParadeDB query syntax
    /// </summary>
    /// <param name="query">Raw user query</param>
    /// <returns>ParadeDB-formatted query string</returns>
    public static string Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var tokens = new List<string>();
        var currentToken = new StringBuilder();
        var inQuotes = false;

        // Tokenize the query, preserving quoted phrases
        for (int i = 0; i < query.Length; i++)
        {
            char c = query[i];

            if (c == '"')
            {
                if (inQuotes)
                {
                    // End of quoted phrase
                    tokens.Add($"\"{currentToken}\"");
                    currentToken.Clear();
                    inQuotes = false;
                }
                else
                {
                    // Start of quoted phrase
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    inQuotes = true;
                }
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                // Space outside quotes = token separator
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }

        // Add remaining token
        if (currentToken.Length > 0)
        {
            if (inQuotes)
            {
                // Unclosed quote - treat as phrase anyway
                tokens.Add($"\"{currentToken}\"");
            }
            else
            {
                tokens.Add(currentToken.ToString());
            }
        }

        // Build ParadeDB query from tokens
        var result = new StringBuilder();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // Skip if it's just an operator word
            if (token.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("OR", StringComparison.OrdinalIgnoreCase))
            {
                result.Append($" {token.ToUpper()} ");
                continue;
            }

            // Add the token (escape if needed, handle prefix)
            if (token.StartsWith('"') && token.EndsWith('"'))
            {
                // Phrase search - keep quotes
                result.Append(token);
            }
            else
            {
                // Regular term or prefix search
                var escapedToken = EscapeSpecialChars(token);
                result.Append(escapedToken);
            }

            // Add default OR between terms if no explicit operator
            if (i < tokens.Count - 1)
            {
                var nextToken = i + 1 < tokens.Count ? tokens[i + 1] : "";
                if (!nextToken.Equals("AND", StringComparison.OrdinalIgnoreCase) &&
                    !nextToken.Equals("OR", StringComparison.OrdinalIgnoreCase) &&
                    !token.Equals("AND", StringComparison.OrdinalIgnoreCase) &&
                    !token.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    result.Append(" OR ");
                }
            }
        }

        var finalQuery = result.ToString().Trim();

        // If query is empty or just whitespace, return wildcard
        return string.IsNullOrWhiteSpace(finalQuery) ? "*" : finalQuery;
    }

    /// <summary>
    /// Escape special characters in search terms (except * for prefix matching)
    /// </summary>
    private static string EscapeSpecialChars(string term)
    {
        if (string.IsNullOrEmpty(term))
            return term;

        // ParadeDB special chars that need escaping (except *)
        // Keep * for prefix matching (alex*)
        var specialChars = new[] { '+', '-', '=', '!', '(', ')', '{', '}', '[', ']', '^', '~', '?', ':', '\\', '/' };

        var escaped = term;
        foreach (var ch in specialChars)
        {
            escaped = escaped.Replace(ch.ToString(), $"\\{ch}");
        }

        return escaped;
    }
}

