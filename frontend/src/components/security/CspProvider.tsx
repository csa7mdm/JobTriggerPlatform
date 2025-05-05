import React from 'react';
import { Helmet } from 'react-helmet-async';
import { generateCspDirectives } from '../../utils/security/csp';

interface CspProviderProps {
  children: React.ReactNode;
  allowUpload?: boolean;
  allowForms?: boolean;
  additionalConnectSrc?: string[];
}

/**
 * CspProvider component that allows route-specific CSP modifications
 * 
 * Use this component in routes that need special permissions beyond the default CSP
 * such as file upload forms or integration with external services.
 */
const CspProvider: React.FC<CspProviderProps> = ({
  children,
  allowUpload = false,
  allowForms = false,
  additionalConnectSrc = []
}) => {
  // Start with base CSP directives
  let cspContent = generateCspDirectives();
  
  // Add route-specific exceptions if needed
  if (allowUpload) {
    // Add media-src for file uploads
    cspContent = cspContent.replace(
      "img-src 'self' data:;",
      "img-src 'self' data: blob:; media-src 'self' blob:;"
    );
  }
  
  if (allowForms) {
    // Allow form submission to specified domains
    cspContent = cspContent.replace(
      "form-action 'self';",
      "form-action 'self' https://api.deployment-portal.example.com;"
    );
  }
  
  if (additionalConnectSrc.length > 0) {
    // Add additional connect-src entries
    const connectSrcPattern = /connect-src\s+([^;]+);/;
    const match = cspContent.match(connectSrcPattern);
    
    if (match && match[1]) {
      const updatedConnectSrc = `${match[1]} ${additionalConnectSrc.join(' ')}`;
      cspContent = cspContent.replace(
        connectSrcPattern,
        `connect-src ${updatedConnectSrc};`
      );
    }
  }
  
  return (
    <>
      <Helmet>
        <meta
          http-equiv="Content-Security-Policy"
          content={cspContent}
        />
      </Helmet>
      {children}
    </>
  );
};

export default CspProvider;