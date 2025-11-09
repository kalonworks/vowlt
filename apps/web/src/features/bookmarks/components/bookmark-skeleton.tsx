import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function BookmarkSkeleton() {
  return (
    <Card className="h-full flex flex-col">
      <CardHeader>
        <div className="space-y-2">
          <Skeleton className="h-6 w-3/4" />
          <Skeleton className="h-3 w-1/2" />
        </div>
      </CardHeader>

      <CardContent className="flex-1">
        <div className="space-y-2">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      </CardContent>

      <CardFooter className="flex flex-col items-start gap-3">
        <div className="flex gap-2">
          <Skeleton className="h-5 w-16" />
          <Skeleton className="h-5 w-20" />
        </div>
        <div className="flex items-center justify-between w-full">
          <Skeleton className="h-3 w-24" />
          <div className="flex gap-2">
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-8 w-8" />
          </div>
        </div>
      </CardFooter>
    </Card>
  );
}
