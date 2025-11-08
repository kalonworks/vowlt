using Vowlt.Api.Features.Search.DTOs;
using Vowlt.Api.Shared.Models;

namespace Vowlt.Api.Features.Search.Services;

public interface ISearchService
{
    Task<Result<SearchResponse>> SearchAsync(
        Guid userId,
        SearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SearchResponse>> FindSimilarAsync(
        Guid userId,
        Guid bookmarkId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

