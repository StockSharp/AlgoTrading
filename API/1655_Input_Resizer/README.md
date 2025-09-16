# Input Resizer

Utility strategy that adds resizable borders to active dialog windows and optionally remembers their size. Ported from MetaTrader4 script **InputResizer**.

## Parameters
- `Remember Size` – store window size for next time.
- `Individual` – store size per window title, otherwise one size for all.
- `Init Maximized` – maximize window on first run.
- `Init Custom` – use custom size on first run.
- `Init X`, `Init Y` – initial position when `Init Custom` is enabled.
- `Init Width`, `Init Height` – initial dimensions when `Init Custom` is enabled.
- `Sleep Time` – delay in milliseconds between window checks.
- `Weekend Mode` – run even when no market data is received.

## Logic
The strategy launches a background loop that monitors the foreground window. When a new dialog appears it adds resizable borders and applies the configured start mode. If remembering is enabled, the last size is stored and restored next time the same window opens.

This utility does not place trades and can run alongside other strategies.
