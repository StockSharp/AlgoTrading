# Deep Drawdown MA Strategy

## Overview
The Deep Drawdown MA Strategy is a direct conversion of the MetaTrader 5 expert advisor “Deep Drawdown MA (barabashkakvn's edition)” into the StockSharp high-level API. The strategy trades moving-average crossovers while applying a break-even mechanism designed to protect trades that have slipped into drawdown. The StockSharp version retains the configurable moving-average parameters, the ability to limit the number of aggregated entries, and the option to immediately liquidate losing trades on a signal reversal.

## Trading Logic
- **Indicators**: Two moving averages with individual periods, price sources, and historical shifts. Both averages share the same smoothing method (SMA, EMA, SMMA, or LWMA).
- **Entry Conditions**:
  - **Long**: The shifted fast average rises above the shifted slow average. The strategy adds the configured order volume (and covers any short exposure) when the last entry was not long and the maximum position cap is not exceeded.
  - **Short**: The shifted fast average falls below the shifted slow average. The strategy sells the configured volume (and covers any long exposure) when the previous entry was not short and the maximum position cap allows it.
- **Exit Conditions**:
  - **Longs**: When the fast average crosses back below the slow average the position is either closed immediately (`CloseLosses = true`) or marked for a break-even exit. During a break-even exit the strategy waits until the candle close returns to the average entry price before flattening.
  - **Shorts**: Mirrored behaviour—on a bullish crossover the position is either closed instantly or armed with a break-even target that triggers once price falls back to the average entry.
- **Position Tracking**: Average entry price and the last opened direction are reconstructed from own trades so that the high-level API can reproduce the MQL behaviour.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Order size for each market operation. | 0.1 |
| `MaxPositions` | Maximum number of aggregated lots per direction (net exposure). | 5 |
| `CloseLosses` | Close losing trades immediately on a reversal instead of waiting for break-even. | false |
| `FastMaPeriod` / `SlowMaPeriod` | Length of the fast and slow moving averages. | 10 / 30 |
| `FastMaShift` / `SlowMaShift` | Historical shift applied to each moving average (emulates the MT5 shift argument). | 3 / 0 |
| `FastPriceType` / `SlowPriceType` | Price source used by each moving average (Close, Open, High, Low, Median, Typical, Weighted). | Close |
| `MaMethod` | Smoothing method shared by both moving averages (SMA, EMA, SMMA, LWMA). | SMA |
| `CandleType` | Candle series used for calculations. | 15-minute candles |

## Conversion Notes
- The original MetaTrader robot could hold hedged long and short positions simultaneously. StockSharp strategies operate on net positions; therefore, the converted version enforces aggregated exposure while still respecting the maximum position count.
- Break-even protection is implemented with internal flags rather than MT5 order modifications. The strategy monitors candle closes and exits at the reconstructed average entry price.
- Moving-average “shift” parameters are reproduced by keeping a short queue of recent indicator values, which mirrors the MT5 `shift` argument without calling low-level indicator buffers.

## Usage
1. Attach the strategy to the desired security and set `OrderVolume`, candle type, and moving-average parameters to match your target market.
2. Enable trading once the strategy is running and the candle subscription is online.
3. Monitor the break-even flags through the logs: trades will be flattened automatically once the price returns to the averaged entry.

## Risk Management
- Use `CloseLosses = true` to force fast liquidation of losing trades when the averages reverse.
- Tune `MaxPositions` to cap the aggregated exposure after consecutive alternating entries.
- Combine the strategy with account-level risk controls available in StockSharp (e.g., `StartProtection`) for additional safeguards.

## Files
- `CS/DeepDrawdownMaStrategy.cs` – C# implementation using the StockSharp high-level API.
- `README.md`, `README_ru.md`, `README_cn.md` – Multilingual documentation of strategy behaviour and parameters.
