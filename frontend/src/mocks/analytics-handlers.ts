import { http, HttpResponse, delay } from 'msw';

// Generate mock data for job statistics
const generateMockJobStats = () => {
  // Create date array for the last 30 days
  const dates = Array.from({ length: 30 }, (_, i) => {
    const date = new Date();
    date.setDate(date.getDate() - i);
    return date.toISOString().split('T')[0];
  }).reverse();
  
  // Generate random success and failure counts for each date
  const trendsData = dates.map(date => {
    const successCount = Math.floor(Math.random() * 20) + 5;
    const failureCount = Math.floor(Math.random() * 10);
    
    return { date, successCount, failureCount };
  });
  
  // Calculate totals
  const totalSuccess = trendsData.reduce((sum, day) => sum + day.successCount, 0);
  const totalFailure = trendsData.reduce((sum, day) => sum + day.failureCount, 0);
  const totalJobs = totalSuccess + totalFailure;
  
  // Calculate rates
  const successRate = totalSuccess / totalJobs;
  const failureRate = totalFailure / totalJobs;
  
  // Generate environment distribution
  const jobsByEnvironment = {
    production: Math.floor(Math.random() * 50) + 50,
    staging: Math.floor(Math.random() * 40) + 30,
    development: Math.floor(Math.random() * 100) + 100,
    test: Math.floor(Math.random() * 80) + 20,
  };
  
  // Generate tag distribution
  const tags = ['deploy', 'backup', 'test', 'build', 'analysis', 'cleanup', 'maintenance', 'monitoring'];
  const mostFrequentTags = tags.map(tag => ({
    tag,
    count: Math.floor(Math.random() * 50) + 1,
  })).sort((a, b) => b.count - a.count);
  
  return {
    totalJobs,
    successRate,
    failureRate,
    averageDuration: Math.floor(Math.random() * 600) + 120, // in seconds
    jobsByEnvironment,
    mostFrequentTags,
    trendsData,
  };
};

// Generate mock data for tag-specific statistics
const generateMockTagStats = (tag: string) => {
  // Create date array for the last 30 days
  const dates = Array.from({ length: 30 }, (_, i) => {
    const date = new Date();
    date.setDate(date.getDate() - i);
    return date.toISOString().split('T')[0];
  }).reverse();
  
  // Generate random counts for each date
  return dates.map(date => ({
    date,
    count: Math.floor(Math.random() * 10) + (tag === 'deploy' ? 5 : 1), // More for 'deploy' tag
  }));
};

// Analytics API handlers
export const analyticsHandlers = [
  // Job Statistics API
  http.get('/api/analytics/job-stats', async () => {
    await delay(1500); // Simulate network delay
    return HttpResponse.json(generateMockJobStats());
  }),
  
  // Tag Statistics API
  http.get('/api/analytics/tag-stats/:tag', async ({ params }) => {
    const { tag } = params;
    await delay(800);
    return HttpResponse.json(generateMockTagStats(tag as string));
  }),
];
