import { ReadingStatus } from '../../types/book';

interface StatusFilterProps {
  selectedStatus: ReadingStatus | null;
  onChange: (status: ReadingStatus | null) => void;
  className?: string;
}

interface StatusTab {
  label: string;
  value: ReadingStatus | null;
  color: string;
}

const statusTabs: StatusTab[] = [
  { label: 'All Books', value: null, color: 'text-gray-700' },
  { label: 'Unread', value: ReadingStatus.Unread, color: 'text-gray-700' },
  { label: 'Reading', value: ReadingStatus.Reading, color: 'text-blue-700' },
  { label: 'Read', value: ReadingStatus.Read, color: 'text-green-700' },
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
