import { ReactNode } from 'react';
import { Library, Plus, MessageSquare, Settings, BookOpen, LogOut } from 'lucide-react';
import { NavLink } from '@/components/NavLink';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/auth/useAuth';
import { useNavigate } from 'react-router-dom';
import { Button } from './ui/button';

interface LayoutProps {
  children: ReactNode;
}

const navItems = [
  { title: 'Library', icon: Library, path: '/' },
  { title: 'Add Books', icon: Plus, path: '/add' },
  { title: 'AI Chat', icon: MessageSquare, path: '/chat' },
  { title: 'Settings', icon: Settings, path: '/settings' },
];

const Layout = ({ children }: LayoutProps) => {
  const { logout, user } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout.mutate({}, {
      onSuccess: () => {
        navigate('/auth');
      }
    });
  };

  return (
    <div className='flex h-screen w-full bg-background'>
      {/* Sidebar */}
      <aside className='w-64 bg-sidebar border-r border-sidebar-border flex flex-col'>
        <div className='p-6 border-b border-sidebar-border'>
          <div className='flex items-center gap-2'>
            <BookOpen className='w-8 h-8 text-primary' />
            <h1 className='text-2xl font-bold text-foreground'>intellishelf</h1>
          </div>
        </div>

        <nav className='flex-1 p-4'>
          <ul className='space-y-2'>
            {navItems.map((item) => (
              <li key={item.path}>
                <NavLink
                  to={item.path}
                  end
                  className='flex items-center gap-3 px-4 py-3 rounded-lg text-sidebar-foreground hover:bg-nav-hover transition-colors'
                  activeClassName='bg-sidebar-accent text-sidebar-accent-foreground'
                >
                  <item.icon className='w-5 h-5' />
                  <span className='font-medium'>{item.title}</span>
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        {user && (
          <div className='p-4 border-t border-sidebar-border'>
            <div className='mb-3 px-4'>
              <p className='text-xs text-muted-foreground'>Signed in as</p>
              <p className='text-sm font-medium text-foreground truncate'>{user.email}</p>
            </div>
            <Button
              variant='ghost'
              className='w-full justify-start text-muted-foreground hover:text-foreground'
              onClick={handleLogout}
            >
              <LogOut className='w-5 h-5 mr-3' />
              Logout
            </Button>
          </div>
        )}
      </aside>

      {/* Main Content */}
      <main className='flex-1 overflow-auto'>
        {children}
      </main>
    </div>
  );
};

export default Layout;
