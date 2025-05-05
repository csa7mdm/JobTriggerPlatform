import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { HelmetProvider, Helmet } from 'react-helmet-async'
import { initMocks } from './mocks'
import App from './App'
import theme from './theme'
import { AuthProvider } from './auth'
import { registerPlugin } from './plugins'
import { generateCspDirectives } from './utils/security/csp'
import { CspReporting, SriProvider } from './components/security'

// Import and register plugins that should be available immediately
import notificationPlugin from './plugins/notificationPlugin'
import singleTenantPlugin from './plugins/singleTenantPlugin'

// Register directly imported plugins
registerPlugin(notificationPlugin, {
  autoInitialize: true,
  config: {
    maxNotifications: 50,
    pollingInterval: 30000, // 30 seconds
  },
})

// Register lazy-loaded plugins
registerPlugin(() => import('./plugins/themePlugin'), {
  autoInitialize: true,
  config: {
    defaultTheme: 'system',
    enableUserPreference: true,
  },
})

// Register Single Tenant plugin
registerPlugin(singleTenantPlugin, {
  autoInitialize: true,
  config: {
    allowApiKeyReset: true,
    validationTimeout: 5000 // 5 seconds
  },
})

// Initialize MSW in development mode
if (import.meta.env.MODE === 'development') {
  initMocks().then(() => {
    console.log('[MSW] Mock service worker initialized');
  });
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <HelmetProvider>
      <Helmet>
        <meta charSet="utf-8" />
        <title>Deployment Portal</title>
        <meta name="description" content="Job Trigger Platform for managing deployment jobs" />
        
        {/* Content Security Policy */}
        <meta
          http-equiv="Content-Security-Policy"
          content={generateCspDirectives()}
        />
        
        {/* Add CSP reporting in production */}
        {import.meta.env.PROD && <CspReporting reportOnly={true} />}
      </Helmet>
      <SriProvider>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <BrowserRouter>
            <AuthProvider>
              <App />
            </AuthProvider>
          </BrowserRouter>
        </ThemeProvider>
      </SriProvider>
    </HelmetProvider>
  </React.StrictMode>,
)