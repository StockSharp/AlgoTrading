# MA2CCI Adaptive Volume Strategy

## Overview
The MA2CCI strategy is a direct port of the MetaTrader 4 expert advisor originally distributed as "MA2CCI.mq4". The system combines a fast/slow simple moving average (SMA) crossover with a Commodity Channel Index (CCI) zero-line confirmation. Every validated crossover opens a single market position and immediately places an Average True Range (ATR) based protective stop. Position sizing follows the original money-management logic by scaling the order size relative to equity and reducing it after streaks of losing trades.

## Indicators and Data
- **Fast SMA (FMa)** and **Slow SMA (SMa)** on the configured timeframe to detect trend reversals.
- **Commodity Channel Index (CCI)** with the same price stream to confirm momentum direction through zero-line crossings.
- **Average True Range (ATR)** to measure recent volatility and derive the stop-loss distance.
- **Candles** of the chosen timeframe (default 15 minutes) provide the input series for all indicators.

## Trading Rules
- **Long entry**: The fast SMA crosses above the slow SMA while CCI crosses from negative to positive on the same bar, no position is open, and trading is allowed. A market buy order is sent and a stop-loss is armed at `close − ATR × AtrMultiplier`.
- **Short entry**: The fast SMA crosses below the slow SMA while CCI crosses from positive to negative, no position is open. A market sell order is placed with a stop-loss at `close + ATR × AtrMultiplier`.
- **Exit for longs**: If the fast SMA crosses back below the slow SMA the entire long position is closed at market. The protective stop is also cancelled.
- **Exit for shorts**: If the fast SMA crosses back above the slow SMA the short position is covered at market and the stop is cancelled.
- **Stop-loss**: Every new position restores a volatility stop that mirrors the MetaTrader logic. Stops are recalculated only on new entries and are stored as separate conditional orders.

## Position Sizing
- The base lot size starts from the `BaseVolume` parameter (default 0.1 lot).
- If `RiskFraction` is positive the strategy calculates an additional size using `equity × RiskFraction / 1000`, mimicking the original `AccountFreeMargin` formula, and uses the maximum between both values.
- After two or more consecutive losing trades the lot size is reduced by `volume × losses / DecreaseFactor`, replicating the `DcF` drawdown control.
- Volumes are normalized to the instrument's `VolumeStep`.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `FastMaPeriod` | 4 | Fast SMA lookback period. |
| `SlowMaPeriod` | 8 | Slow SMA lookback period. |
| `CciPeriod` | 4 | Commodity Channel Index period. |
| `AtrPeriod` | 4 | Average True Range period used for stop distance. |
| `AtrMultiplier` | 1.0 | Multiplier applied to ATR before placing the stop-loss. |
| `BaseVolume` | 0.1 | Minimum trade size before risk adjustments. |
| `RiskFraction` | 0.02 | Fraction of equity risked per trade (per 1000 currency units). |
| `DecreaseFactor` | 3 | Divisor that controls how fast the size shrinks after losses. |
| `CandleType` | 15-minute candles | Timeframe used for indicators and signals. |

## Notes
- Email notifications from the original expert advisor (`SndMl`) are intentionally omitted.
- Only one position can be open at a time, matching the MetaTrader behaviour of the source code.
- Protective stops are recreated whenever the position flips or closes to keep orphan orders from remaining in the book.
