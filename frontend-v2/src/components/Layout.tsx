import { ReactNode } from "react";
import { Library, Plus, MessageSquare, Settings, BookOpen } from "lucide-react";
import { NavLink } from "@/components/NavLink";
import { cn } from "@/lib/utils";

interface LayoutProps {
  children: ReactNode;
}

const navItems = [
  { title: "Library", icon: Library, path: "/" },
  { title: "Add Books", icon: Plus, path: "/add" },
  { title: "AI Chat", icon: MessageSquare, path: "/chat" },
  { title: "Settings", icon: Settings, path: "/settings" },
];

const Layout = ({ children }: LayoutProps) => {
  return (
    <div className="flex h-screen w-full bg-background">
      {/* Sidebar */}
      <aside className="w-64 bg-sidebar border-r border-sidebar-border flex flex-col">
        <div className="p-6 border-b border-sidebar-border">
          <div className="flex items-center gap-2">
            <BookOpen className="w-8 h-8 text-primary" />
            <h1 className="text-2xl font-bold text-foreground">intellishelf</h1>
          </div>
        </div>

        <nav className="flex-1 p-4">
          <ul className="space-y-2">
            {navItems.map((item) => (
              <li key={item.path}>
                <NavLink
                  to={item.path}
                  end
                  className="flex items-center gap-3 px-4 py-3 rounded-lg text-sidebar-foreground hover:bg-nav-hover transition-colors"
                  activeClassName="bg-sidebar-accent text-sidebar-accent-foreground"
                >
                  <item.icon className="w-5 h-5" />
                  <span className="font-medium">{item.title}</span>
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className="p-4 border-t border-sidebar-border">
          <div className="text-xs text-muted-foreground">
            Your digital library
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        {children}
      </main>
    </div>
  );
};

export default Layout;
