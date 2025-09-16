# MACD Zero Filter Take Profit Strategy

## Overview
This strategy replicates the original MetaTrader 5 expert "Robot_MACD" that trades MACD signal line crossovers with additional zero-line filters. It operates on a single instrument and looks for momentum reversals confirmed by the position of the MACD line relative to zero. A fixed-distance take profit is attached to every order, mirroring the point-based profit target from the original implementation.

## Data and Indicators
- **Primary data**: single candle subscription (default 5-minute timeframe). The timeframe can be changed with the `CandleType` parameter to suit the traded market.
- **Indicators**:
  - `MovingAverageConvergenceDivergenceSignal` (MACD + signal + histogram). The defaults are 12/26 EMAs with a 9-period signal line, matching the MQL parameters.

## Trading Logic
1. Wait for the MACD calculation to provide both current and previous values of the MACD and signal lines.
2. Identify bullish and bearish crossovers:
   - **Bullish crossover**: previous MACD ≤ previous signal **and** current MACD > current signal.
   - **Bearish crossover**: previous MACD ≥ previous signal **and** current MACD < current signal.
3. **Position management**:
   - Close a long position when a bearish crossover appears.
   - Close a short position when a bullish crossover appears.
4. **Entry conditions** (only when no position is open and there is sufficient capital):
   - Enter long on a bullish crossover **while both MACD and signal remain below zero**.
   - Enter short on a bearish crossover **while both MACD and signal remain above zero**.
5. Attach a fixed take profit at order registration time by calling `StartProtection` with an absolute distance measured in price points. The distance equals the configured point value multiplied by the security's price step.

## Risk Management
- Every order has an attached take profit defined by `TakeProfitPoints`. There is no stop-loss in the base logic, preserving parity with the source EA.
- The strategy checks whether the portfolio value is at least `MinimumCapitalPerVolume * VolumePerTrade` before placing a new order. This emulates the free-margin guard (`FreeMargin() < 1000 * Lots`) from the MQL version.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `MacdFast` | Fast EMA period for MACD. | 12 |
| `MacdSlow` | Slow EMA period for MACD. | 26 |
| `MacdSignal` | Signal line smoothing period. | 9 |
| `TakeProfitPoints` | Take profit distance expressed in price points. | 300 |
| `VolumePerTrade` | Trading volume (lots) used for each entry. | 1 |
| `MinimumCapitalPerVolume` | Minimum portfolio value required per traded lot. | 1000 |
| `CandleType` | Candle type (timeframe) used to feed the MACD indicator. | 5-minute candles |

## Implementation Notes
- Orders are executed with `BuyMarket`/`SellMarket`, identical to the EA that used market orders via `CTrade`.
- The zero-line filters guard against entering trades in the opposite half of the MACD histogram, just as in the MQL script.
- The portfolio value check relies on `Portfolio.CurrentValue`. If the trading environment does not supply this value the guard automatically passes, which keeps the strategy usable for simulated accounts.
- The chart drawing section plots candles, the MACD indicator, and trade markers when a chart area is available in the host application.
