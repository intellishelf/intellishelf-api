import { ReadingStatus } from '../../types/book';
import { STATUS_CONFIG } from '../../constants';

interface StatusBadgeProps {
  status: ReadingStatus;
  className?: string;
}

export const StatusBadge = ({ status, className = '' }: StatusBadgeProps) => {
  const config = STATUS_CONFIG[status];

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.badgeClass} ${className}`}
    >
      {config.label}
    </span>
  );
};
