using System;

namespace Vowlt.Api.Features.Embedding.Services;

public interface IEmbeddingService
  {
      Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default);
      Task<float[][]> EmbedTextsAsync(string[] texts, CancellationToken cancellationToken = default);
  }

