from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer
from sentence_transformers import CrossEncoder
from typing import List
import logging
import time

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="Vowlt Embedding Service",
    description="Converts text to semantic embeddings using sentence-transformers",
    version="1.0.0"
)

# Load model on startup (happens once)
logger.info("Loading sentence-transformers model...")
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')
logger.info("Model loaded successfully!")
logger.info("Loading cross-encoder model...")
cross_encoder = CrossEncoder('cross-encoder/ms-marco-MiniLM-L-6-v2')
logger.info("Model loaded successfully!")

# Request/Response models
class EmbedRequest(BaseModel):
    texts: List[str] = Field(..., min_items=1, max_items=100, description="List of texts to embed (max 100)")

class EmbedResponse(BaseModel):
    embeddings: List[List[float]]
    model: str
    dimensions: int
    processing_time_ms: float
class RerankRequest(BaseModel):
      query: str
      texts: List[str]
class RerankItem(BaseModel):
      index: int
      score: float
class RerankResponse(BaseModel):
      scores: List[RerankItem]


@app.get("/")
async def root():
    """Health check endpoint"""
    return {
        "service": "Vowlt Embedding Service",
        "status": "healthy",
        "model": "all-MiniLM-L6-v2",
        "dimensions": 384
    }

@app.get("/health")
async def health():
    """Detailed health check"""
    return {
        "status": "healthy",
        "model_loaded": model is not None,
        "model_name": "sentence-transformers/all-MiniLM-L6-v2"
    }

@app.post("/embed", response_model=EmbedResponse)
async def embed(request: EmbedRequest):
    """
    Convert texts to embeddings.
    
    Returns:
        - embeddings: List of 384-dimensional vectors
        - model: Model name used
        - dimensions: Vector dimensions (384)
        - processing_time_ms: Time taken to process
    """
    try:
        start_time = time.time()

        logger.info(f"Embedding {len(request.texts)} texts")

        # Convert texts to embeddings
        embeddings = model.encode(
            request.texts,
            convert_to_numpy=True,
            show_progress_bar=False
        )

        # Convert numpy array to list for JSON serialization
        embeddings_list = embeddings.tolist()

        processing_time = (time.time() - start_time) * 1000  # Convert to ms

        logger.info(f"Embeddings generated in {processing_time:.2f}ms")

        return EmbedResponse(
            embeddings=embeddings_list,
            model="all-MiniLM-L6-v2",
            dimensions=384,
            processing_time_ms=processing_time
        )

    except Exception as e:
        logger.error(f"Error embedding texts: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Embedding failed: {str(e)}")
    
@app.post("/rerank", response_model=RerankResponse)
async def rerank(request: RerankRequest):
    """
    Rerank texts using cross-encoder for more accurate relevance scoring.
    Returns scores in range [0, 1] where higher = more relevant.
    """
    try:
        # Create pairs of (query, text) for the cross-encoder
        pairs = [[request.query, text] for text in request.texts]

        # Get scores from cross-encoder
        scores = cross_encoder.predict(pairs)

        # Normalize scores to [0, 1] range using sigmoid
        import numpy as np
        normalized_scores = 1 / (1 + np.exp(-scores))

        # Create response with index and score
        results = [
            RerankItem(index=i, score=float(score))
            for i, score in enumerate(normalized_scores)
        ]

        return RerankResponse(scores=results)

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Reranking failed: {str(e)}")