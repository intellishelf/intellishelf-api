import { Link } from 'react-router-dom';
import { BookOpen, LogOut, User } from 'lucide-react';
import { useAuth } from '../../hooks/auth/useAuth';
import { Menu } from '@headlessui/react';
import toast from 'react-hot-toast';

export const Header = () => {
  const { user, logout } = useAuth();

  const handleLogout = async () => {
    try {
      await logout();
      toast.success('Logged out successfully');
    } catch (error) {
      toast.error('Failed to logout');
    }
  };

  return (
    <header className="bg-white border-b border-gray-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo */}
          <Link to="/dashboard" className="flex items-center gap-2 hover:opacity-80 transition-opacity">
            <div className="bg-primary-600 p-2 rounded-lg">
              <BookOpen className="w-6 h-6 text-white" />
            </div>
            <span className="text-xl font-bold text-gray-900">IntelliShelf</span>
          </Link>

          {/* User Menu */}
          <div className="flex items-center gap-4">
            <Menu as="div" className="relative">
              <Menu.Button className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors">
                <div className="w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center">
                  <User className="w-5 h-5 text-primary-600" />
                </div>
                <span className="text-sm font-medium text-gray-700 hidden sm:block">
                  {user?.email}
                </span>
              </Menu.Button>

              <Menu.Items className="absolute right-0 mt-2 w-56 origin-top-right bg-white rounded-lg shadow-lg border border-gray-200 focus:outline-none z-10">
                <div className="p-2">
                  <div className="px-3 py-2 border-b border-gray-200 mb-2">
                    <p className="text-sm font-medium text-gray-900">Signed in as</p>
                    <p className="text-sm text-gray-600 truncate">{user?.email}</p>
                  </div>

                  <Menu.Item>
                    {({ active }) => (
                      <button
                        onClick={handleLogout}
                        className={`${
                          active ? 'bg-gray-100' : ''
                        } flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 rounded-md transition-colors`}
                      >
                        <LogOut className="w-4 h-4" />
                        Sign out
                      </button>
                    )}
                  </Menu.Item>
                </div>
              </Menu.Items>
            </Menu>
          </div>
        </div>
      </div>
    </header>
  );
};
