/**
 * HTML Sanitization utilities using DOMPurify
 * 
 * These utilities help prevent XSS attacks by sanitizing any HTML content
 * before it's rendered in the application.
 */
import DOMPurify from 'dompurify';

/**
 * Configuration for DOMPurify
 * 
 * These settings determine what HTML elements and attributes are allowed
 * after sanitization.
 */
const DEFAULT_CONFIG: DOMPurify.Config = {
  ALLOWED_TAGS: [
    // Basic text formatting
    'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'span', 'strong', 'em', 'u', 'br',
    // Lists
    'ul', 'ol', 'li',
    // Containers
    'div', 'section', 'article', 'main', 'header', 'footer',
    // Links
    'a',
    // Tables
    'table', 'thead', 'tbody', 'tr', 'th', 'td',
    // Other
    'pre', 'code', 'blockquote', 'hr',
  ],
  ALLOWED_ATTR: [
    // Global attributes
    'id', 'class', 'title', 'dir', 'lang', 'tabindex',
    // Link attributes
    'href', 'target', 'rel',
    // Accessibility
    'aria-*', 'role',
    // Data attributes for component interactions
    'data-*'
  ],
  // Prevent usage of DOM clobbering
  FORBID_CONTENTS: ['script', 'style', 'iframe', 'form'],
  // Only allow http/https URLs (prevents javascript: URLs)
  ALLOWED_URI_REGEXP: /^(?:(?:(?:f|ht)tps?|mailto):|[^a-z]|[a-z+.-]+(?:[^a-z+.:-]|$))/i,
  // Return DOM nodes instead of HTML string (for React use)
  RETURN_DOM: false,
  RETURN_DOM_FRAGMENT: false,
  // Don't allow potentially dangerous features
  ADD_TAGS: [],
  ADD_ATTR: [],
  USE_PROFILES: { html: true }
};

/**
 * Restricted configuration with minimum allowed tags for user-generated content
 */
const RESTRICTED_CONFIG: DOMPurify.Config = {
  ...DEFAULT_CONFIG,
  ALLOWED_TAGS: ['p', 'span', 'strong', 'em', 'u', 'br', 'ul', 'ol', 'li', 'a'],
  ALLOWED_ATTR: ['href', 'target', 'rel', 'aria-*']
};

/**
 * Sanitizes HTML content using the default configuration
 * 
 * @param html HTML content to sanitize
 * @returns Sanitized HTML string
 */
export const sanitizeHtml = (html: string): string => {
  if (!html) return '';
  return DOMPurify.sanitize(html, DEFAULT_CONFIG);
};

/**
 * Sanitizes HTML content with restricted configuration
 * 
 * Use this for user-generated content with minimal formatting
 * 
 * @param html HTML content to sanitize
 * @returns Sanitized HTML string
 */
export const sanitizeUserHtml = (html: string): string => {
  if (!html) return '';
  return DOMPurify.sanitize(html, RESTRICTED_CONFIG);
};

/**
 * Sanitizes a plain text string to prevent script injection when used as HTML
 * 
 * @param text Plain text to sanitize
 * @returns Sanitized text string
 */
export const sanitizePlainText = (text: string): string => {
  if (!text) return '';
  return DOMPurify.sanitize(text, { ALLOWED_TAGS: [] });
};

// Export a React hook for sanitizing HTML
export const useSanitizedHtml = (html: string, restricted: boolean = false): string => {
  return restricted ? sanitizeUserHtml(html) : sanitizeHtml(html);
};

export default {
  sanitizeHtml,
  sanitizeUserHtml,
  sanitizePlainText,
  useSanitizedHtml
};