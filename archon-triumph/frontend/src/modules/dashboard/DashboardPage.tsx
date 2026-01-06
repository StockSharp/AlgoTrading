import React from 'react';

export const DashboardPage: React.FC = () => {
  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: '2fr 1fr',
        gridTemplateRows: 'minmax(0, 1fr) minmax(0, 1fr)',
        gap: 16,
        height: '100%',
      }}
    >
      <div style={{ gridColumn: '1 / span 1', gridRow: '1 / span 1', border: '1px solid #1f2937', borderRadius: 8, padding: 12 }}>
        Backend Health Panel
      </div>
      <div style={{ border: '1px solid #1f2937', borderRadius: 8, padding: 12 }}>
        Brokers Panel
      </div>
      <div style={{ border: '1px solid #1f2937', borderRadius: 8, padding: 12 }}>
        Plugins Panel
      </div>
      <div style={{ gridColumn: '1 / span 2', border: '1px solid #1f2937', borderRadius: 8, padding: 12 }}>
        Activity Feed
      </div>
    </div>
  );
};
