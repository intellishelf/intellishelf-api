import { useAuth } from '../hooks/auth/useAuth';
import { Layout } from '../components/layout/Layout';

export const DashboardPage = () => {
  const { user } = useAuth();

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="bg-white rounded-lg shadow-sm p-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-4">
            Welcome to IntelliShelf
          </h1>
          <p className="text-gray-600">
            You are logged in as: <span className="font-semibold">{user?.email}</span>
          </p>
          <p className="text-gray-500 mt-4">
            Books list coming soon in Phase 2...
          </p>
        </div>
      </div>
    </Layout>
  );
};
