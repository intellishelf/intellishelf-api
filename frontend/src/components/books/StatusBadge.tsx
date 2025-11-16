import { ReadingStatus } from '../../types/book';

interface StatusBadgeProps {
  status: ReadingStatus;
  className?: string;
}

const statusConfig = {
  [ReadingStatus.Unread]: {
    label: 'Unread',
    className: 'bg-gray-100 text-gray-700',
  },
  [ReadingStatus.Reading]: {
    label: 'Reading',
    className: 'bg-blue-100 text-blue-700',
  },
  [ReadingStatus.Read]: {
    label: 'Read',
    className: 'bg-green-100 text-green-700',
  },
};

export const StatusBadge = ({ status, className = '' }: StatusBadgeProps) => {
  const config = statusConfig[status];

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.className} ${className}`}
    >
      {config.label}
    </span>
  );
};
