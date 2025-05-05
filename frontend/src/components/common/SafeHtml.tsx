import React from 'react';
import { sanitizeHtml, sanitizeUserHtml } from '../../utils/sanitization/htmlSanitizer';

interface SafeHtmlProps {
  html: string;
  restrictedMode?: boolean;
  className?: string;
  testId?: string;
}

/**
 * Renders HTML content after sanitizing it with DOMPurify
 * to prevent XSS attacks
 * 
 * @param props.html - HTML content to sanitize and render
 * @param props.restrictedMode - Use more restricted sanitization rules
 * @param props.className - Optional CSS class name for styling
 * @param props.testId - Optional data-testid for testing
 */
const SafeHtml: React.FC<SafeHtmlProps> = ({
  html,
  restrictedMode = false,
  className,
  testId = 'safe-html',
}) => {
  // If no HTML content, return null
  if (!html) return null;
  
  // Apply appropriate sanitization based on mode
  const sanitizedHtml = restrictedMode
    ? sanitizeUserHtml(html)
    : sanitizeHtml(html);
  
  return (
    <div
      className={className}
      data-testid={testId}
      dangerouslySetInnerHTML={{ __html: sanitizedHtml }}
    />
  );
};

export default SafeHtml;