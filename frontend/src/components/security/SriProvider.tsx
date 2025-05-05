import React from 'react';
import { Helmet } from 'react-helmet-async';
import { FONTS, ICONS, SCRIPTS } from '../../utils/security/sri';

interface SriProviderProps {
  children: React.ReactNode;
  includeFonts?: boolean;
  includeIcons?: boolean;
  includeScripts?: boolean;
}

/**
 * Provides Subresource Integrity (SRI) for external resources
 * 
 * This component adds integrity hashes to external resources in the document head
 * to ensure they haven't been tampered with.
 */
const SriProvider: React.FC<SriProviderProps> = ({
  children,
  includeFonts = true,
  includeIcons = true,
  includeScripts = false
}) => {
  return (
    <>
      <Helmet>
        {/* Add preconnect hints for performance */}
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link rel="preconnect" href="https://cdn.jsdelivr.net" crossOrigin="anonymous" />
        
        {/* Font stylesheets with SRI */}
        {includeFonts && FONTS.map((font, index) => (
          <link
            key={`font-${index}`}
            href={font.url}
            rel={font.rel || 'stylesheet'}
            integrity={font.integrity}
            crossOrigin={font.crossOrigin}
          />
        ))}
        
        {/* Icon stylesheets with SRI */}
        {includeIcons && ICONS.map((icon, index) => (
          <link
            key={`icon-${index}`}
            href={icon.url}
            rel={icon.rel || 'stylesheet'}
            integrity={icon.integrity}
            crossOrigin={icon.crossOrigin}
          />
        ))}
        
        {/* External scripts with SRI */}
        {includeScripts && SCRIPTS.map((script, index) => (
          <script
            key={`script-${index}`}
            src={script.url}
            integrity={script.integrity}
            crossOrigin={script.crossOrigin}
            type={script.type || 'text/javascript'}
          />
        ))}
      </Helmet>
      {children}
    </>
  );
};

export default SriProvider;