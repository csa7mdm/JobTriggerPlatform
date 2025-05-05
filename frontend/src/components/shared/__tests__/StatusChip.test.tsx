import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import StatusChip from '../StatusChip';

describe('StatusChip', () => {
  it('renders with correct text', () => {
    render(<StatusChip status="running" />);
    expect(screen.getByText('Running')).toBeInTheDocument();
  });

  it('applies success color for success status', () => {
    const { container } = render(<StatusChip status="success" />);
    expect(container.firstChild).toHaveClass('MuiChip-colorSuccess');
  });

  it('applies error color for failed status', () => {
    const { container } = render(<StatusChip status="failed" />);
    expect(container.firstChild).toHaveClass('MuiChip-colorError');
  });

  it('applies primary color for running status', () => {
    const { container } = render(<StatusChip status="running" />);
    expect(container.firstChild).toHaveClass('MuiChip-colorPrimary');
  });

  it('applies default color for unknown status', () => {
    const { container } = render(<StatusChip status="unknown" />);
    expect(container.firstChild).toHaveClass('MuiChip-colorDefault');
  });

  it('capitalizes the first letter of the status', () => {
    render(<StatusChip status="completed" />);
    expect(screen.getByText('Completed')).toBeInTheDocument();
  });
});