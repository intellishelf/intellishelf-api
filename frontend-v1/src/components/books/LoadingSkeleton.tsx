export const LoadingSkeleton = () => {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-6">
      {Array.from({ length: 10 }).map((_, index) => (
        <div key={index} className="animate-pulse">
          <div className="aspect-[2/3] bg-gray-200 rounded-lg mb-4"></div>
          <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
          <div className="h-3 bg-gray-200 rounded w-1/2 mb-2"></div>
          <div className="h-5 bg-gray-200 rounded w-1/3"></div>
        </div>
      ))}
    </div>
  );
};
