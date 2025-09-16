# Ilan 1.6 Dynamic Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Ilan 1.6 Dynamic strategy is a classic grid and martingale expert advisor. It opens an initial trade in a selected direction and places additional orders every time price moves against the position by a fixed step. Volume of new orders grows geometrically by a lot exponent. All positions in the basket are closed when price returns to the average entry price plus a take-profit distance. A trailing stop can optionally protect profits if price moves far enough in the favorable direction.

The algorithm relies only on price movement and does not use indicators. Because position size increases after each adverse move, the system carries high risk but can capture quick reversals.

## Details

- **Entry**
  - First order is opened in the configured direction.
  - Additional orders are added every `PipStep` points against the current position, up to `MaxTrades`.
  - Each new order volume = `InitialVolume * LotExponent^N`.
- **Exit**
  - Close all when price touches `AveragePrice ± TakeProfit`.
  - Optional trailing stop starts after `TrailStart` points of profit and follows price at `TrailStop` distance.
- **Position management**
  - Only long or only short series at a time.
  - After closing the basket the strategy restarts from the initial direction.
- **Parameters**
  - `InitialVolume` – volume of the first order (default 1).
  - `LotExponent` – multiplier for subsequent order size (default 1.6).
  - `PipStep` – distance in points between grid levels (default 30).
  - `TakeProfit` – profit target from average price in points (default 10).
  - `MaxTrades` – maximum number of active orders (default 10).
  - `StartLong` – open first trade as long if true (default true).
  - `UseTrailingStop` – enable trailing stop (default false).
  - `TrailStart` – profit in points to start trailing (default 10).
  - `TrailStop` – trailing distance in points (default 10).
  - `CandleType` – timeframe of candles (default 1 minute).
- **Filters**
  - Category: Grid
  - Direction: Both
  - Indicators: None
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
