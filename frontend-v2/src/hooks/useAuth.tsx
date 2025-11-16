import { createContext, useContext, useState, useEffect, ReactNode } from 'react';

interface User {
  email: string;
  name: string;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, name: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    const storedUser = localStorage.getItem('auth_user');
    if (storedUser) {
      setUser(JSON.parse(storedUser));
    }
  }, []);

  const login = async (email: string, password: string) => {
    // Simple client-side authentication (demo purposes only)
    const storedUsers = localStorage.getItem('registered_users');
    const users = storedUsers ? JSON.parse(storedUsers) : [];
    
    const existingUser = users.find((u: any) => u.email === email && u.password === password);
    
    if (existingUser) {
      const userData = { email: existingUser.email, name: existingUser.name };
      setUser(userData);
      localStorage.setItem('auth_user', JSON.stringify(userData));
    } else {
      throw new Error('Invalid credentials');
    }
  };

  const register = async (email: string, password: string, name: string) => {
    const storedUsers = localStorage.getItem('registered_users');
    const users = storedUsers ? JSON.parse(storedUsers) : [];
    
    if (users.find((u: any) => u.email === email)) {
      throw new Error('User already exists');
    }
    
    const newUser = { email, password, name };
    users.push(newUser);
    localStorage.setItem('registered_users', JSON.stringify(users));
    
    const userData = { email, name };
    setUser(userData);
    localStorage.setItem('auth_user', JSON.stringify(userData));
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('auth_user');
  };

  return (
    <AuthContext.Provider value={{ user, login, register, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
