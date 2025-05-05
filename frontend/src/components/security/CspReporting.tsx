import React, { useEffect } from 'react';
import { Helmet } from 'react-helmet-async';
import { generateCspDirectives } from '../../utils/security/csp';

interface CspReportingProps {
  reportUri?: string;
  reportOnly?: boolean;
}

/**
 * Component to configure CSP violation reporting
 * 
 * This is typically used in production to monitor for potential CSP violations
 * without necessarily blocking content (when in report-only mode).
 */
const CspReporting: React.FC<CspReportingProps> = ({
  reportUri = 'https://csp-reports.deployment-portal.example.com/report',
  reportOnly = true
}) => {
  // Generate CSP directives with reporting
  const cspContent = generateCspDirectives() + ` report-uri ${reportUri};`;
  
  // Track CSP violations in development for debugging
  useEffect(() => {
    if (import.meta.env.MODE === 'development') {
      document.addEventListener('securitypolicyviolation', (e) => {
        console.warn('CSP Violation:', {
          blockedURI: e.blockedURI,
          violatedDirective: e.violatedDirective,
          originalPolicy: e.originalPolicy,
          disposition: e.disposition
        });
      });
    }
  }, []);
  
  return (
    <Helmet>
      {reportOnly ? (
        <meta
          http-equiv="Content-Security-Policy-Report-Only"
          content={cspContent}
        />
      ) : (
        <meta
          http-equiv="Content-Security-Policy"
          content={cspContent}
        />
      )}
    </Helmet>
  );
};

export default CspReporting;