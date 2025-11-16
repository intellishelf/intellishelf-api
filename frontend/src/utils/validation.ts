/**
 * Email validation regex
 * Matches standard email format: user@domain.extension
 */
export const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Minimum password length requirement
 */
export const PASSWORD_MIN_LENGTH = 5;

/**
 * Validates an email address
 * @param email - The email to validate
 * @returns Error message if invalid, undefined if valid
 */
export const validateEmail = (email: string): string | undefined => {
  if (!email) {
    return 'Email is required';
  }

  if (!EMAIL_REGEX.test(email)) {
    return 'Email is invalid';
  }

  return undefined;
};

/**
 * Validates a password
 * @param password - The password to validate
 * @returns Error message if invalid, undefined if valid
 */
export const validatePassword = (password: string): string | undefined => {
  if (!password) {
    return 'Password is required';
  }

  if (password.length < PASSWORD_MIN_LENGTH) {
    return `Password must be at least ${PASSWORD_MIN_LENGTH} characters`;
  }

  return undefined;
};

/**
 * Validates both email and password
 * @param email - The email to validate
 * @param password - The password to validate
 * @returns Object with email and password error messages (undefined if valid)
 */
export const validateLoginForm = (email: string, password: string) => {
  return {
    email: validateEmail(email),
    password: validatePassword(password),
  };
};
