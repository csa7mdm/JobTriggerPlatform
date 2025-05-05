/**
 * Subresource Integrity (SRI) utilities
 * 
 * These utilities help manage SRI hashes for external resources
 * to ensure they haven't been tampered with.
 */

/**
 * Interface for a resource with SRI hash
 */
export interface ResourceWithIntegrity {
  url: string;
  integrity: string;
  crossOrigin: 'anonymous' | 'use-credentials' | '';
  type?: string; // For preload links
  as?: string; // For preload links
  rel?: string; // For links
}

/**
 * CDN fonts with their SRI hashes
 * 
 * These hashes must be updated whenever the external resources change.
 * You can generate SRI hashes using https://www.srihash.org/
 */
export const FONTS: ResourceWithIntegrity[] = [
  {
    url: 'https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap',
    integrity: 'sha384-C6RzsynM9kWDrMNeT87bh95OGNyZPhcTNXj1NW7RuBCsyN/o0jlpcV8Qyq46cDfL',
    crossOrigin: 'anonymous',
    rel: 'stylesheet'
  },
  {
    url: 'https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap',
    integrity: 'sha384-m6yCzJlJh4T8/FLLuDSiVD+7S5ByFzQnMHvg4bIpKb8Y2QiQZQhyZLpTNlCCYUt7',
    crossOrigin: 'anonymous',
    rel: 'stylesheet'
  }
];

/**
 * CDN icons with their SRI hashes
 */
export const ICONS: ResourceWithIntegrity[] = [
  {
    url: 'https://fonts.googleapis.com/icon?family=Material+Icons',
    integrity: 'sha384-s5SbRt7RmYPgxl9sYSA3DeD39E1hkOL7yCxw8skjRM9UBJ0hiUVeCWp4ZdKUvlRQ',
    crossOrigin: 'anonymous',
    rel: 'stylesheet'
  },
  {
    url: 'https://cdn.jsdelivr.net/npm/@mdi/font@7.2.96/css/materialdesignicons.min.css',
    integrity: 'sha384-Vb8YYtTl+9n6FVDRTLmjFS+JPXvh1J3pd/T6TTzxmdzQ41LRIJLdzm3nL0jKYgTg',
    crossOrigin: 'anonymous',
    rel: 'stylesheet'
  }
];

/**
 * CDN scripts with their SRI hashes
 */
export const SCRIPTS: ResourceWithIntegrity[] = [
  {
    url: 'https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js',
    integrity: 'sha384-zsCLZcPWkDhnOF+NwgGTUP1PKVhGC+xHe2E3I3AWVqCRFnEEOcw3rLbOK3/X8Mdp',
    crossOrigin: 'anonymous',
    type: 'text/javascript'
  }
];

/**
 * Generate appropriate HTML attributes for SRI
 */
export const getSriAttributes = (resource: ResourceWithIntegrity): Record<string, string> => {
  const attributes: Record<string, string> = {
    integrity: resource.integrity,
    crossOrigin: resource.crossOrigin
  };
  
  if (resource.type) {
    attributes.type = resource.type;
  }
  
  if (resource.as) {
    attributes.as = resource.as;
  }
  
  if (resource.rel) {
    attributes.rel = resource.rel;
  }
  
  return attributes;
};

export default {
  FONTS,
  ICONS,
  SCRIPTS,
  getSriAttributes
};