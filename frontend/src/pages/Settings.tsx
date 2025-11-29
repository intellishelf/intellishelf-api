import { Card } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { useState, useEffect } from 'react';
import { toast } from 'sonner';
import { useDeleteAccount } from '@/hooks/auth/useDeleteAccount';

type ColorPalette = 'cyan' | 'purple' | 'emerald' | 'amber';

const colorPalettes: Record<ColorPalette, { primary: string; name: string }> = {
  cyan: { primary: '186 100% 53%', name: 'Cyan (Default)' },
  purple: { primary: '270 80% 60%', name: 'Purple' },
  emerald: { primary: '160 84% 39%', name: 'Emerald' },
  amber: { primary: '38 92% 50%', name: 'Amber' },
};

const Settings = () => {
  const [selectedPalette, setSelectedPalette] = useState<ColorPalette>('cyan');
  const [confirmationChecked, setConfirmationChecked] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const deleteAccountMutation = useDeleteAccount();

  useEffect(() => {
    const saved = localStorage.getItem('colorPalette') as ColorPalette;
    if (saved && colorPalettes[saved]) {
      setSelectedPalette(saved);
      applyPalette(saved);
    }
  }, []);

  const applyPalette = (palette: ColorPalette) => {
    const root = document.documentElement;
    const colors = colorPalettes[palette];
    root.style.setProperty('--primary', colors.primary);
    root.style.setProperty('--accent', colors.primary);
    root.style.setProperty('--ring', colors.primary);
    root.style.setProperty('--sidebar-primary', colors.primary);
    root.style.setProperty('--sidebar-ring', colors.primary);
  };

  const handlePaletteChange = (palette: ColorPalette) => {
    setSelectedPalette(palette);
    applyPalette(palette);
    localStorage.setItem('colorPalette', palette);
    toast.success('Color palette updated', {
      description: `Switched to ${colorPalettes[palette].name}`,
    });
  };

  const handleDeleteAccount = () => {
    if (!confirmationChecked) {
      toast.error('Please confirm that you understand this action cannot be undone');
      return;
    }

    deleteAccountMutation.mutate();
  };

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
              Color Palette
            </h2>
            <RadioGroup value={selectedPalette} onValueChange={handlePaletteChange}>
              <div className='space-y-3'>
                {(Object.keys(colorPalettes) as ColorPalette[]).map((palette) => (
                  <div key={palette} className='flex items-center space-x-2'>
                    <RadioGroupItem value={palette} id={palette} />
                    <Label htmlFor={palette} className='cursor-pointer flex items-center gap-2'>
                      <div
                        className='w-4 h-4 rounded-full border border-border'
                        style={{ backgroundColor: `hsl(${colorPalettes[palette].primary})` }}
                      />
                      {colorPalettes[palette].name}
                    </Label>
                  </div>
                ))}
              </div>
            </RadioGroup>
          </Card>

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

          <Card className='bg-card border-destructive p-6'>
            <h2 className='text-xl font-semibold text-destructive mb-2'>
              Danger Zone
            </h2>
            <p className='text-sm text-muted-foreground mb-4'>
              Irreversible actions that will permanently affect your account
            </p>

            <AlertDialog open={isDialogOpen} onOpenChange={(open) => {
              setIsDialogOpen(open);
              if (!open) {
                setConfirmationChecked(false);
              }
            }}>
              <AlertDialogTrigger asChild>
                <Button
                  variant='destructive'
                  className='w-full'
                  disabled={deleteAccountMutation.isPending}
                >
                  Delete Account
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle className='text-destructive'>
                    Delete Account Permanently?
                  </AlertDialogTitle>
                  <AlertDialogDescription className='space-y-4'>
                    <p>
                      This action <span className='font-semibold text-destructive'>cannot be undone</span>.
                      This will permanently delete your account and remove all your data from our servers.
                    </p>
                    <p className='text-sm'>
                      The following data will be permanently deleted:
                    </p>
                    <ul className='text-sm list-disc list-inside space-y-1 ml-2'>
                      <li>All your books and reading lists</li>
                      <li>All book cover images</li>
                      <li>Your account information</li>
                      <li>All session data</li>
                    </ul>

                    <div className='flex items-start space-x-2 bg-destructive/10 p-3 rounded-md border border-destructive/20'>
                      <Checkbox
                        id='confirm-delete'
                        checked={confirmationChecked}
                        onCheckedChange={(checked) => setConfirmationChecked(checked === true)}
                        className='mt-0.5 border-destructive data-[state=checked]:bg-destructive data-[state=checked]:border-destructive'
                      />
                      <label
                        htmlFor='confirm-delete'
                        className='text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer'
                      >
                        I understand that this action is permanent and cannot be undone
                      </label>
                    </div>
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel disabled={deleteAccountMutation.isPending}>
                    Cancel
                  </AlertDialogCancel>
                  <AlertDialogAction
                    onClick={(e) => {
                      e.preventDefault();
                      handleDeleteAccount();
                    }}
                    disabled={!confirmationChecked || deleteAccountMutation.isPending}
                    className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
                  >
                    {deleteAccountMutation.isPending ? 'Deleting...' : 'Delete Account'}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default Settings;
