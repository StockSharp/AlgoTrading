# Test MACD Strategy

## Overview
The **Test MACD Strategy** is a faithful conversion of the MetaTrader `TestMACD` expert advisor into the StockSharp high-level API. It uses the Moving Average Convergence Divergence (MACD) indicator to detect momentum shifts and executes trades whenever the MACD line crosses the signal line on closed candles. The strategy operates on a single instrument and timeframe supplied through the `CandleType` parameter.

## Trading Logic
1. Subscribe to candle data defined by `CandleType` and calculate a MACD indicator with configurable fast, slow, and signal periods.
2. Monitor the MACD value difference (`MACD - Signal`) on every finished candle.
3. Trigger a **bullish entry** when the difference changes sign from non-positive to positive, meaning the MACD line has crossed above the signal line. Any short exposure is closed before opening the long position.
4. Trigger a **bearish entry** when the difference changes sign from non-negative to negative, meaning the MACD line has crossed below the signal line. Any long exposure is closed before opening the short position.
5. All orders are issued at market with a fixed volume configured by the `TradeVolume` parameter.
6. Each entry is automatically protected with stop-loss and take-profit levels expressed in price steps to replicate the point-based risk management from the original expert.

## Risk Management
- Stop-loss and take-profit distances mirror the MetaTrader inputs and are supplied in price steps. If the security lacks `PriceStep` information, the strategy falls back to absolute price distances using `MinPriceStep` or `1` as the multiplier.
- Protective orders are created once, when the strategy starts, via `StartProtection`, ensuring they apply to every subsequent trade without reconfiguration.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FastPeriod` | Fast EMA length used in MACD calculations. | `12` |
| `SlowPeriod` | Slow EMA length used in MACD calculations. | `24` |
| `SignalPeriod` | Signal line EMA length for MACD smoothing. | `9` |
| `StopLossPoints` | Stop-loss distance expressed in price steps. | `90` |
| `TakeProfitPoints` | Take-profit distance expressed in price steps. | `110` |
| `TradeVolume` | Fixed volume for all market orders. | `1` |
| `CandleType` | Candle data type and timeframe subscribed by the strategy. | `30-minute time frame` |

## Usage Notes
- Attach the strategy to a security before starting it so that `PriceStep` and `MinPriceStep` are available.
- Ensure market data is provided for the selected `CandleType`; otherwise the MACD indicator will not form and trading will not occur.
- The strategy logs every crossover event, making it easy to trace trade decisions during backtests.

## Conversion Details
- Original MetaTrader classes `CSignalMACD`, `CTrailingNone`, and `CMoneyFixedLot` are replaced by StockSharp's indicator binding and `StartProtection` mechanisms.
- Logic from `ExtStateMACD` that checked for MACD crossovers is represented by a sign-change detector on the MACD difference between consecutive finished candles.
- Money management is simplified to a fixed volume parameter, closely resembling the fixed-lot behavior of `CMoneyFixedLot` when percent-based sizing is disabled.
