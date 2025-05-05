async function initMocks() {
  if (import.meta.env.MODE === 'development') {
    const { worker } = await import('./browser');
    return worker.start({
      onUnhandledRequest: 'bypass',
    });
  }
  return Promise.resolve();
}

export { initMocks };