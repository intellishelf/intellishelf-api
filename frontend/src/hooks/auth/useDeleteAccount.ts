import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import api from '@/lib/api';

export const useDeleteAccount = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.void('/auth/account', 'DELETE'),
    onSuccess: () => {
      // Clear all cached data
      queryClient.clear();
      toast.success('Account deleted successfully');
      // Redirect to auth page
      navigate('/auth');
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete account: ${error.message}`);
    },
  });
};
