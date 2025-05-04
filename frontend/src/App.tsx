import { Box, Container, Typography } from '@mui/material'
import { Routes, Route } from 'react-router-dom'
import Layout from './components/Layout'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route
          index
          element={
            <Container>
              <Box sx={{ my: 4 }}>
                <Typography variant="h4" component="h1" gutterBottom>
                  Job Trigger Platform
                </Typography>
                <Typography variant="body1">
                  Welcome to the Job Trigger Platform, a deployment management tool.
                </Typography>
              </Box>
            </Container>
          }
        />
      </Route>
    </Routes>
  )
}

export default App
