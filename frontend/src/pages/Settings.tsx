import { Card } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';

const Settings = () => {
  return (
    <div className='h-full overflow-auto'>
      <div className='max-w-2xl mx-auto p-6'>
        <div className='mb-8'>
          <h1 className='text-3xl font-bold text-foreground mb-2'>Settings</h1>
          <p className='text-muted-foreground'>
            Manage your app preferences
          </p>
        </div>

        <div className='space-y-6'>
          <Card className='bg-card border-border p-6'>
            <h2 className='text-xl font-semibold text-foreground mb-4'>
              Display
            </h2>
            <div className='space-y-4'>
              <div className='flex items-center justify-between'>
                <Label htmlFor="grid-view" className='cursor-pointer'>
                  Default grid view
                </Label>
                <Switch id='grid-view' defaultChecked />
              </div>
              <div className='flex items-center justify-between'>
                <Label htmlFor="show-covers" className='cursor-pointer'>
                  Show book covers
                </Label>
                <Switch id='show-covers' defaultChecked />
              </div>
            </div>
          </Card>

          <Card className='bg-card border-border p-6'>
            <h2 className='text-xl font-semibold text-foreground mb-4'>
              AI Chat
            </h2>
            <div className='space-y-4'>
              <div className='flex items-center justify-between'>
                <Label htmlFor="ai-suggestions" className='cursor-pointer'>
                  Enable AI suggestions
                </Label>
                <Switch id='ai-suggestions' defaultChecked />
              </div>
              <div className='flex items-center justify-between'>
                <Label htmlFor="auto-categorize" className='cursor-pointer'>
                  Auto-categorize books
                </Label>
                <Switch id='auto-categorize' />
              </div>
            </div>
          </Card>

          <Card className='bg-card border-border p-6'>
            <h2 className='text-xl font-semibold text-foreground mb-4'>
              Data
            </h2>
            <div className='space-y-4'>
              <Button variant='outline' className='w-full'>
                Export Library
              </Button>
              <Button variant='outline' className='w-full'>
                Import Books
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default Settings;
