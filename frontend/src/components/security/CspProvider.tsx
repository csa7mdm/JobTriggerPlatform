import React from 'react';
import { Helmet } from 'react-helmet-async';

/**
 * Props for the CspProvider component.
 */
interface CspProviderProps {
  /** Child components to be wrapped by the CSP provider */
  children: React.ReactNode;
  
  /** Allow forms (default: true) */
  allowForms?: boolean;
  
  /** Allow inline styles (default: true) */
  allowInlineStyles?: boolean;
  
  /** Additional domains to allow for connect-src */
  additionalConnectSrc?: string[];
  
  /** Additional domains to allow for img-src */
  additionalImgSrc?: string[];
  
  /** Additional domains to allow for script-src */
  additionalScriptSrc?: string[];
}

/**
 * A component that provides Content Security Policy (CSP) configuration
 * for its children using react-helmet-async.
 */
export const CspProvider: React.FC<CspProviderProps> = ({
  children,
  allowForms = true,
  allowInlineStyles = true,
  additionalConnectSrc = [],
  additionalImgSrc = [],
  additionalScriptSrc = [],
}) => {
  // Base CSP directives
  const defaultSrc = ["'self'"];
  const scriptSrc = ["'self'", ...additionalScriptSrc];
  const styleSrc = ["'self'", 'https://fonts.googleapis.com'];
  const imgSrc = ["'self'", 'data:', ...additionalImgSrc];
  const fontSrc = ["'self'", 'https://fonts.gstatic.com'];
  const connectSrc = ["'self'", 'http://localhost:5000', ...additionalConnectSrc];
  
  // Add 'unsafe-inline' for styles if allowed
  if (allowInlineStyles) {
    styleSrc.push("'unsafe-inline'");
  }
  
  // Add form-action if forms are allowed
  const formAction = allowForms ? ["'self'"] : ["'none'"];
  
  // Build the complete CSP string
  const cspContent = [
    `default-src ${defaultSrc.join(' ')}`,
    `script-src ${scriptSrc.join(' ')}`,
    `style-src ${styleSrc.join(' ')}`,
    `img-src ${imgSrc.join(' ')}`,
    `font-src ${fontSrc.join(' ')}`,
    `connect-src ${connectSrc.join(' ')}`,
    `form-action ${formAction.join(' ')}`,
    "object-src 'none'",
    "frame-ancestors 'none'",
    "base-uri 'self'",
    "upgrade-insecure-requests"
  ].join('; ');
  
  return (
    <>
      <Helmet>
        <meta http-equiv="Content-Security-Policy" content={cspContent} />
      </Helmet>
      {children}
    </>
  );
};

// Also export the component as default
export default CspProvider;
