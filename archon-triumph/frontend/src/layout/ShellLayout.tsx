import React from 'react';

export const ShellLayout: React.FC<React.PropsWithChildren> = ({ children }) => {
  return (
    <div style={{ height: '100vh', display: 'flex', background: '#020617', color: '#e5e7eb' }}>
      <aside style={{ width: 220, borderRight: '1px solid #111827', padding: 16 }}>
        <div style={{ fontWeight: 600, marginBottom: 16 }}>ARCHON TRIUMPH</div>
        <nav>
          <div>Dashboard</div>
        </nav>
      </aside>
      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <header style={{ height: 40, borderBottom: '1px solid #111827', padding: '8px 16px' }}>
          Environment: LOCAL
        </header>
        <div style={{ flex: 1, padding: 16 }}>{children}</div>
        <footer style={{ height: 28, borderTop: '1px solid #111827', padding: '4px 16px', fontSize: 12 }}>
          Status bar placeholder
        </footer>
      </main>
    </div>
  );
};
