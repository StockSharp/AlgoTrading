# Fractal WPR Strategy

This strategy uses the Williams %R oscillator to generate trading signals based on crossings of overbought and oversold levels. It is adapted from an MQL5 expert advisor and demonstrates a simple momentum reversal system.

## How It Works

1. A Williams %R indicator with configurable period is calculated on the selected timeframe.
2. Two horizontal levels define extreme zones:
   - `HighLevel` marks the overbought area (default −30).
   - `LowLevel` marks the oversold area (default −70).
3. When `Trend` is set to `Direct`:
   - Crossing downward through `LowLevel` opens a long position and closes any short position.
   - Crossing upward through `HighLevel` opens a short position and closes any long position.
4. When `Trend` is set to `Against`, the reactions to crossings are reversed.
5. Optional parameters allow enabling or disabling opening and closing of long or short positions separately.
6. Stop‑loss and take‑profit distances in ticks are applied using the high‑level protection API.

Only completed candles are processed to avoid reacting to intrabar noise.

## Parameters

- `WprPeriod` – Williams %R calculation period.
- `HighLevel` – threshold for the overbought zone.
- `LowLevel` – threshold for the oversold zone.
- `Trend` – trading mode (`Direct` or `Against`).
- `BuyPositionOpen` – allow opening long positions.
- `SellPositionOpen` – allow opening short positions.
- `BuyPositionClose` – allow closing long positions.
- `SellPositionClose` – allow closing short positions.
- `StopLossTicks` – stop‑loss distance in ticks.
- `TakeProfitTicks` – take‑profit distance in ticks.
- `CandleType` – candle timeframe used for analysis.

## Indicators

- Williams %R

