using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Search.Models;

namespace Vowlt.Api.Features.Search.Services;

/// <summary>
/// BM25 keyword search implementation using ParadeDB
/// </summary>
public class KeywordSearchService(
    VowltDbContext context,
    ILogger<KeywordSearchService> logger) : IKeywordSearchService
{
    public async Task<List<KeywordSearchResult>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? domain = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse query to support AND/OR, phrases, prefix matching
            var parsedQuery = QueryParser.Parse(query);

            logger.LogInformation(
                "Keyword search: UserId={UserId}, Query={Query}, ParsedQuery={ParsedQuery}, Limit={Limit}",
                userId, query, parsedQuery, limit);

            // Build SQL query with ParadeDB search
            var sql = @"
                  SELECT 
                      b.""Id"" as BookmarkId,
                      paradedb.score(b.""Id"") as Bm25Score,
                      ROW_NUMBER() OVER (ORDER BY paradedb.score(b.""Id"") DESC) as Rank
                  FROM ""Bookmarks"" b
                  WHERE b.""Id"" @@@ paradedb.parse(@query)
                      AND b.""UserId"" = @userId";

            // Add optional filters
            if (fromDate.HasValue)
                sql += @" AND b.""CreatedAt"" >= @fromDate";

            if (toDate.HasValue)
                sql += @" AND b.""CreatedAt"" <= @toDate";

            if (!string.IsNullOrWhiteSpace(domain))
                sql += @" AND b.""Domain"" = @domain";

            sql += @"
                  ORDER BY paradedb.score(b.""Id"") DESC
                  LIMIT @limit";

            // Execute query
            var results = await context.Database
                .SqlQueryRaw<KeywordSearchResultRaw>(
                    sql,
                    new Npgsql.NpgsqlParameter("@query", parsedQuery),
                    new Npgsql.NpgsqlParameter("@userId", userId),
                    new Npgsql.NpgsqlParameter("@limit", limit),
                    new Npgsql.NpgsqlParameter("@fromDate", (object?)fromDate ?? DBNull.Value),
                    new Npgsql.NpgsqlParameter("@toDate", (object?)toDate ?? DBNull.Value),
                    new Npgsql.NpgsqlParameter("@domain", (object?)domain ?? DBNull.Value))
                .ToListAsync(cancellationToken);

            logger.LogInformation(
                "Keyword search completed: Found {Count} results",
                results.Count);

            return results.Select(r => new KeywordSearchResult
            {
                BookmarkId = r.BookmarkId,
                Bm25Score = r.Bm25Score,
                Rank = r.Rank
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during keyword search: UserId={UserId}, Query={Query}",
                userId, query);
            throw;
        }
    }

    /// <summary>
    /// Raw result from SQL query (EF Core needs this for SqlQueryRaw)
    /// </summary>
    private class KeywordSearchResultRaw
    {
        public Guid BookmarkId { get; set; }
        public double Bm25Score { get; set; }
        public int Rank { get; set; }
    }
}
