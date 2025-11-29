import { Card } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Button } from '@/components/ui/button';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { useState, useEffect } from 'react';
import { toast } from 'sonner';

type ColorPalette = 'cyan' | 'purple' | 'emerald' | 'amber';

const colorPalettes: Record<ColorPalette, { primary: string; name: string }> = {
  cyan: { primary: '186 100% 53%', name: 'Cyan (Default)' },
  purple: { primary: '270 80% 60%', name: 'Purple' },
  emerald: { primary: '160 84% 39%', name: 'Emerald' },
  amber: { primary: '38 92% 50%', name: 'Amber' },
};

const Settings = () => {
  const [selectedPalette, setSelectedPalette] = useState<ColorPalette>('cyan');

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
        </div>
      </div>
    </div>
  );
};

export default Settings;
