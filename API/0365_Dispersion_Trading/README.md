# Dispersion Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The dispersion trading strategy exploits periods when an equity index and its constituents diverge. When the average pairwise correlation between index members drops below a threshold, the strategy buys the individual stocks and shorts the index, betting that correlations will mean‑revert.

Daily candles feed a rolling correlation window. If correlations recover above the threshold, all positions are closed. A minimum trade value is enforced to avoid tiny orders.

## Details

- **Universe**: One index security plus its constituent stocks.
- **Signal**: Open a dispersion trade when the average correlation of constituents is below `CorrThreshold`.
- **Rebalance**: Correlation checked every day.
- **Positioning**: Long constituents and short the index while the signal is active.
- **Parameters**:
  - `Constituents` – list of component securities.
  - `LookbackDays` – window size for correlation calculation.
  - `CorrThreshold` – correlation level that triggers trades.
  - `MinTradeUsd` – minimum order value in USD.
  - `CandleType` – timeframe for candles (default: 1 day).
- **Note**: The example omits transaction costs and assumes equal weighting.
