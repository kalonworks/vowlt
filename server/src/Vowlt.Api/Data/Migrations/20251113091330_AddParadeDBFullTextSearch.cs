using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vowlt.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParadeDBFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_search extension
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_search;");

            // Create ParadeDB BM25 index for full-text search
            migrationBuilder.Sql(@"
                CREATE INDEX bookmarks_search_idx ON ""Bookmarks""
                USING bm25 (""Id"", ""Title"", ""Description"", ""Notes"", ""FullText"", ""Tags"", ""GeneratedTags"", ""Url"")
                WITH (
                    key_field='Id',
                    text_fields='{
                        ""Title"": {
                            ""tokenizer"": { ""type"": ""default"" },
                            ""record"": ""position"",
                            ""fast"": true
                        },
                        ""Description"": {
                            ""tokenizer"": { ""type"": ""default"" },
                            ""record"": ""position""
                        },
                        ""Notes"": {
                            ""tokenizer"": { ""type"": ""default"" },
                            ""record"": ""position""
                        },
                        ""FullText"": {
                            ""tokenizer"": { ""type"": ""default"" },
                            ""record"": ""basic""
                        },
                        ""Tags"": {
                            ""tokenizer"": { ""type"": ""keyword"" },
                            ""record"": ""position""
                        },
                        ""GeneratedTags"": {
                            ""tokenizer"": { ""type"": ""keyword"" },
                            ""record"": ""position""
                        },
                        ""Url"": {
                            ""tokenizer"": { ""type"": ""keyword"" },
                            ""fast"": true
                        }
                    }'
                );
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the ParadeDB BM25 index
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS bookmarks_search_idx;");
        }
    }
}
