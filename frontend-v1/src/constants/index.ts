import { ReadingStatus } from '../types/book';

/**
 * Query keys for React Query
 * Centralizes all query keys to avoid typos and improve maintainability
 */
export const QUERY_KEYS = {
  books: ['books'] as const,
  book: (id: string) => ['books', id] as const,
  searchBooks: (searchTerm: string, status?: ReadingStatus) =>
    ['books', 'search', { searchTerm, status }] as const,
} as const;

/**
 * Status configuration for reading statuses
 * Includes labels and styling for badges and filters
 */
export const STATUS_CONFIG = {
  [ReadingStatus.Unread]: {
    label: 'Unread',
    badgeClass: 'bg-gray-100 text-gray-700',
    filterClass: 'text-gray-700 hover:bg-gray-100',
  },
  [ReadingStatus.Reading]: {
    label: 'Reading',
    badgeClass: 'bg-blue-100 text-blue-700',
    filterClass: 'text-blue-700 hover:bg-blue-100',
  },
  [ReadingStatus.Read]: {
    label: 'Read',
    badgeClass: 'bg-green-100 text-green-700',
    filterClass: 'text-green-700 hover:bg-green-100',
  },
} as const;

/**
 * Default pagination values
 */
export const PAGINATION_DEFAULTS = {
  PAGE_SIZE: 24,
  PAGE_SIZE_OPTIONS: [12, 24, 48, 96] as const,
  INITIAL_PAGE: 1,
} as const;

/**
 * Form field constraints
 */
export const FORM_CONSTRAINTS = {
  TITLE_MAX_LENGTH: 500,
  DESCRIPTION_MAX_LENGTH: 2000,
  ANNOTATION_MAX_LENGTH: 2000,
  ISBN_LENGTH: 10, // for ISBN-10
  ISBN13_LENGTH: 13, // for ISBN-13
} as const;
