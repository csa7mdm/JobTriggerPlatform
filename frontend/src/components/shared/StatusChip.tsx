import React from 'react';
import { Chip, ChipProps } from '@mui/material';
import { getStatusColor } from '../../utils/helpers';

interface StatusChipProps extends Omit<ChipProps, 'color'> {
  status: string;
}

const StatusChip: React.FC<StatusChipProps> = ({ status, ...props }) => {
  const color = getStatusColor(status);
  const label = status.charAt(0).toUpperCase() + status.slice(1);

  return (
    <Chip
      {...props}
      label={label}
      color={color}
      size="small"
    />
  );
};

export default StatusChip;