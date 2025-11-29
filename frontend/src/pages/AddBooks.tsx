import { Card } from '@/components/ui/card';
import BookForm from '@/components/books/BookForm';
import { useNavigate } from 'react-router-dom';

const AddBooks = () => {
  const navigate = useNavigate();

  return (
    <div className='h-full overflow-auto'>
      <div className='max-w-2xl mx-auto p-6'>
        <div className='mb-8'>
          <h1 className='text-3xl font-bold text-foreground mb-2'>Add Books</h1>
          <p className='text-muted-foreground'>
            Add new books to your digital library
          </p>
        </div>

        <Card className='bg-card border-border p-6'>
          <BookForm onSuccess={() => navigate('/')} />
        </Card>
      </div>
    </div>
  );
};

export default AddBooks;
