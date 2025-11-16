import { ReadingStatus } from '../../types/book';
import { STATUS_CONFIG } from '../../constants';

interface StatusFilterProps {
  selectedStatus: ReadingStatus | null;
  onChange: (status: ReadingStatus | null) => void;
  className?: string;
}

interface StatusTab {
  label: string;
  value: ReadingStatus | null;
}

const statusTabs: StatusTab[] = [
  { label: 'All Books', value: null },
  { label: STATUS_CONFIG[ReadingStatus.Unread].label, value: ReadingStatus.Unread },
  { label: STATUS_CONFIG[ReadingStatus.Reading].label, value: ReadingStatus.Reading },
  { label: STATUS_CONFIG[ReadingStatus.Read].label, value: ReadingStatus.Read },
];

export const StatusFilter: React.FC<StatusFilterProps> = ({
  selectedStatus,
  onChange,
  className = '',
}) => {
  return (
    <div className={`flex flex-wrap gap-2 ${className}`}>
      {statusTabs.map((tab) => {
        const isActive = selectedStatus === tab.value;
        return (
          <button
            key={tab.label}
            onClick={() => onChange(tab.value)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
              isActive
                ? 'bg-indigo-600 text-white shadow-sm'
                : 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50'
            }`}
            aria-pressed={isActive}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
};
