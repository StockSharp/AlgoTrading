import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { ShellLayout } from './layout/ShellLayout';
import { DashboardPage } from './modules/dashboard/DashboardPage';

export default function App() {
  return (
    <ShellLayout>
      <Routes>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </ShellLayout>
  );
}
