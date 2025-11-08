# Multi-stage build for smaller final image
FROM python:3.11-slim as builder

# Set working directory
WORKDIR /app

# Install build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first (for layer caching)
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Download model during build (caches in image)
RUN python -c "from sentence_transformers import SentenceTransformer;SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')"

# Final stage - smaller image
FROM python:3.11-slim

# Create non-root user for security
RUN useradd -m -u 1000 vowlt && \
    mkdir -p /app && \
    chown -R vowlt:vowlt /app

WORKDIR /app

# Copy installed packages from builder
COPY --from=builder /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin
COPY --from=builder /root/.cache /home/vowlt/.cache

# Copy application code
COPY --chown=vowlt:vowlt main.py .

# Switch to non-root user
USER vowlt

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')" || exit 1

# Run the application
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
