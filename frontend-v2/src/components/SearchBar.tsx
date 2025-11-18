import { Search, X } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

const SearchBar = ({
  value,
  onChange,
  placeholder = "Search books by title, author, ISBN, or description..."
}: SearchBarProps) => {
  const handleClear = () => {
    onChange("");
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Escape") {
      handleClear();
    }
  };

  return (
    <div className='relative w-full max-w-2xl'>
      <Search className='absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground' />
      <Input
        type='text'
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        className='pl-12 pr-12 h-12 bg-search-bg border-border text-foreground placeholder:text-muted-foreground rounded-lg focus-visible:ring-primary'
        aria-label="Search books"
      />
      {value && (
        <Button
          type='button'
          variant='ghost'
          size='icon'
          onClick={handleClear}
          className='absolute right-2 top-1/2 -translate-y-1/2 h-8 w-8 text-muted-foreground hover:text-foreground'
          aria-label="Clear search"
        >
          <X className='h-4 w-4' />
        </Button>
      )}
    </div>
  );
};

export default SearchBar;
