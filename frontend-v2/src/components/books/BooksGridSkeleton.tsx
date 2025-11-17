import { Skeleton } from '@/components/ui/skeleton';

interface BooksGridSkeletonProps {
  count?: number;
}

const BooksGridSkeleton = ({ count = 12 }: BooksGridSkeletonProps) => {
  return (
    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-6">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="space-y-3">
          <Skeleton className="aspect-[2/3] w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-3 w-3/4" />
        </div>
      ))}
    </div>
  );
};

export default BooksGridSkeleton;
