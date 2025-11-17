import { ReadingStatus } from '@/types/book';
import { Badge } from '@/components/ui/badge';

interface StatusBadgeProps {
  status: ReadingStatus;
  className?: string;
}

const statusConfig = {
  [ReadingStatus.Unread]: {
    label: 'Unread',
    variant: 'secondary' as const,
  },
  [ReadingStatus.Reading]: {
    label: 'Reading',
    variant: 'default' as const,
  },
  [ReadingStatus.Read]: {
    label: 'Read',
    variant: 'outline' as const,
  },
};

const StatusBadge = ({ status, className }: StatusBadgeProps) => {
  const config = statusConfig[status];

  return (
    <Badge variant={config.variant} className={className}>
      {config.label}
    </Badge>
  );
};

export default StatusBadge;
