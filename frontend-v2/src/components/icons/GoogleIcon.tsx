import { siGoogle } from 'simple-icons';

interface GoogleIconProps {
  className?: string;
}

export const GoogleIcon = ({ className }: GoogleIconProps) => (
  <svg
    role='img'
    viewBox="0 0 24 24"
    className={className}
    fill="currentColor"
    xmlns="http://www.w3.org/2000/svg"
  >
    <title>Google</title>
    <path d={siGoogle.path} />
  </svg>
);
