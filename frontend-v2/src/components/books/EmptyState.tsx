import { BookOpen } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useNavigate } from 'react-router-dom';

interface EmptyStateProps {
  title?: string;
  description?: string;
  showAddButton?: boolean;
}

const EmptyState = ({
  title = 'No books in your library yet',
  description = 'Add your first book to get started',
  showAddButton = true,
}: EmptyStateProps) => {
  const navigate = useNavigate();

  return (
    <div className="flex flex-col items-center justify-center h-full py-12 text-center">
      <BookOpen className="w-16 h-16 text-muted-foreground mb-4" />
      <h3 className="text-lg font-semibold text-foreground mb-2">{title}</h3>
      <p className="text-sm text-muted-foreground mb-6 max-w-sm">
        {description}
      </p>
      {showAddButton && (
        <Button onClick={() => navigate('/add')}>Add Your First Book</Button>
      )}
    </div>
  );
};

export default EmptyState;
