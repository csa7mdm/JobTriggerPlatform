# HTML Sanitization and XSS Prevention

This document outlines the HTML sanitization approach implemented in the Deployment Portal application to prevent Cross-Site Scripting (XSS) attacks.

## Overview

The application uses DOMPurify to sanitize any HTML content before it's rendered to the DOM. This prevents malicious scripts from being executed in the browser context.

## Implementation Details

### 1. DOMPurify Integration

We've integrated DOMPurify with the following approach:

- **Utility Functions**: Sanitization utilities are available in `src/utils/sanitization/htmlSanitizer.ts`
- **React Component**: The `SafeHtml` component in `src/components/common` provides a React-friendly way to safely render HTML content
- **Consistent Configuration**: We maintain a common configuration across the application to ensure sanitization rules are applied consistently

### 2. Sanitization Levels

We provide multiple sanitization levels for different use cases:

| Function | Purpose | Use Case |
|----------|---------|----------|
| `sanitizeHtml` | Standard sanitization for most content | Documentation, rich text content |
| `sanitizeUserHtml` | Restricted sanitization for user content | User-generated content, comments |
| `sanitizePlainText` | Removes all HTML tags | Plain text fields that should never contain HTML |

### 3. SafeHtml Component

This component provides a convenient way to render sanitized HTML in React:

```jsx
<SafeHtml 
  html={content} 
  restrictedMode={true} // Optional: Apply more restricted rules
  className="custom-class" // Optional: CSS class
/>
```

### 4. Allowed HTML Tags and Attributes

The application restricts HTML based on these configurations:

**Default Configuration:**
- **Tags**: Basic text formatting, lists, tables, links, and containers
- **Attributes**: Standard presentation attributes, ARIA attributes, and data attributes
- **Blocked**: Scripts, styles, iframes, forms, and other potentially dangerous elements

**Restricted Configuration:**
- **Tags**: Only basic text formatting (p, span, em, strong) and links
- **Attributes**: A minimal set needed for basic formatting

## Usage Guidelines

### When to Use SafeHtml

Use the `SafeHtml` component whenever you need to render content that:
1. Contains or might contain HTML markup
2. Comes from user input or external sources
3. Is stored in the database and could potentially contain malicious code

### When to Use sanitizePlainText

Use `sanitizePlainText` for:
1. Command outputs, log messages, or other content that should be treated as plain text
2. Content that is displayed in contexts where HTML should never be allowed
3. User inputs before processing or storing them

### Examples

#### Rendering Job Descriptions (may contain formatting)

```jsx
<SafeHtml 
  html={jobDetails.description} 
  restrictedMode={true}
  className="job-description"
/>
```

#### Sanitizing Command Outputs

```jsx
<Typography component="pre">
  {sanitizePlainText(commandOutput)}
</Typography>
```

## Security Considerations

1. **Never Bypass Sanitization**: Always use DOMPurify for any content that might contain HTML, even if you think it's safe.
2. **Test Sanitization**: Regularly test sanitization with known XSS attack vectors.
3. **Keep DOMPurify Updated**: Regularly update DOMPurify to benefit from the latest security patches.
4. **Defense in Depth**: Sanitization is one part of a defense-in-depth strategy. Also use CSP, proper encoding, and other security measures.

## Related Security Measures

This sanitization approach works in conjunction with:
- Content Security Policy (CSP) to further mitigate XSS risks
- Subresource Integrity (SRI) for external resources
- CORS settings to control cross-origin requests
- HTTP security headers for overall application security