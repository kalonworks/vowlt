import { formatDistanceToNow } from "date-fns";
import { ExternalLink, Sparkles } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { SearchResult } from "../types";

interface SearchResultCardProps {
  result: SearchResult;
}

export function SearchResultCard({ result }: SearchResultCardProps) {
  const displayTitle = result.title || result.url;
  const timeAgo = formatDistanceToNow(new Date(result.createdAt), {
    addSuffix: true,
  });

  // Convert similarity score to percentage
  const similarityPercent = Math.round(result.similarityScore * 100);

  // Color-code based on similarity score
  const getScoreColor = (score: number) => {
    if (score >= 0.8)
      return "bg-green-500/10 text-green-700 border-green-500/20";
    if (score >= 0.6) return "bg-blue-500/10 text-blue-700 border-green-500/20";
    return "bg-gray-500/10 text-gray-700 border-gray-500/20";
  };

  return (
    <Card className="h-full flex flex-col hover:shadow-md transition-shadow">
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <CardTitle className="text-lg line-clamp-2">
                <a
                  href={result.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="hover:underline inline-flex items-center gap-1"
                >
                  {displayTitle}
                  <ExternalLink className="h-4 w-4 flex-shrink-0" />
                </a>
              </CardTitle>
            </div>
            {result.domain && (
              <CardDescription className="text-xs truncate">
                {result.domain}
              </CardDescription>
            )}
          </div>
          <Badge
            variant="outline"
            className={`flex items-center gap-1 shrink-0 ${getScoreColor(result.similarityScore)}`}
          >
            <Sparkles className="h-3 w-3" />
            {similarityPercent}%
          </Badge>
        </div>
      </CardHeader>

      {result.description && (
        <CardContent className="flex-1">
          <p className="text-sm text-muted-foreground line-clamp-3">
            {result.description}
          </p>
        </CardContent>
      )}

      <CardFooter>
        <span className="text-xs text-muted-foreground">Saved {timeAgo}</span>
      </CardFooter>
    </Card>
  );
}
