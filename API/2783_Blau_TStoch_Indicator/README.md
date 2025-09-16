# Blau TStoch Indicator Strategy

## Overview
- Port of the MetaTrader 5 expert advisor `Exp_BlauTStochI` to the StockSharp high level API.
- Trades the Blau Triple Stochastic Index (William Blau) on configurable timeframes.
- Supports two execution modes: **Breakdown** (zero line breakouts) and **Twist** (slope reversals).
- Position permissions reproduce the original expert advisor flags (independent toggles for opening/closing long and short trades).

## Indicator construction
- Calculates a momentum series as `applied price - lowest` over `MomentumLength` bars and its range `highest - lowest`.
- Applies three consecutive smoothing stages to both numerator and denominator.
- Supported smoothing methods: Exponential (EMA), Simple (SMA), Smoothed/Running (SMMA), and Linear Weighted (LWMA).
- The original MQL options (JJMA, JurX, ParMA, T3, VIDYA, AMA) are **not** reproduced; the `Phase` parameter is retained for compatibility but ignored.
- Applied price options match the MQL enumerations (close, open, high, low, median, typical, weighted, simple, quarted, trend-following variants, DeMark).
- Final indicator value: `100 * smoothedStoch / smoothedRange - 50`.

## Trading rules
### Breakdown mode
- Inspect the indicator on the bar defined by `SignalBar` (default 1, i.e. the last closed candle).
- **Long entry:** previous value (`SignalBar+1`) above zero **and** current value (`SignalBar`) crosses below or equals zero.
- **Short entry:** previous value below zero **and** current value crosses above or equals zero.
- **Long exit:** previous value below zero and long exits permitted.
- **Short exit:** previous value above zero and short exits permitted.

### Twist mode
- **Long entry:** indicator rising (`value[SignalBar+1] < value[SignalBar+2]`) and the latest value not lower than the previous one.
- **Short entry:** indicator falling (`value[SignalBar+1] > value[SignalBar+2]`) and the latest value not higher than the previous one.
- **Long exit:** indicator slope turns downward (`value[SignalBar+1] > value[SignalBar+2]`).
- **Short exit:** indicator slope turns upward (`value[SignalBar+1] < value[SignalBar+2]`).

### Position management
- Entries reverse existing opposite positions by adding the absolute position size to the configured `Volume`.
- Exits close the full existing position with market orders.
- Trade processing is performed only on finished candles and after the indicator is fully formed.

## Risk management
- Optional stop-loss and take-profit measured in price steps (`StopLossPoints`, `TakeProfitPoints`).
- Both are implemented via `StartProtection` and can be disabled by setting the distance to zero.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Data type/timeframe for calculations. | 4-hour candles |
| `Smoothing` | Smoothing method (EMA/SMA/SMMA/LWMA). | EMA |
| `MomentumLength` | Lookback for highest/lowest detection. | 20 |
| `FirstSmoothing` | Length of smoothing stage 1. | 5 |
| `SecondSmoothing` | Length of smoothing stage 2. | 8 |
| `ThirdSmoothing` | Length of smoothing stage 3. | 3 |
| `Phase` | Kept for compatibility (ignored). | 15 |
| `PriceType` | Applied price constant. | Close |
| `SignalBar` | Bar shift used for signal evaluation (>= 1). | 1 |
| `Mode` | Trading mode (Breakdown/Twist). | Twist |
| `AllowLongEntries` | Enable long entries. | true |
| `AllowShortEntries` | Enable short entries. | true |
| `AllowLongExits` | Enable closing long trades. | true |
| `AllowShortExits` | Enable closing short trades. | true |
| `TakeProfitPoints` | Take-profit distance in steps (0 disables). | 2000 |
| `StopLossPoints` | Stop-loss distance in steps (0 disables). | 1000 |

## Differences from the MT5 expert
- Advanced smoothing algorithms from SmoothAlgorithms.mqh are not implemented; choose among EMA/SMA/SMMA/LWMA.
- Money management (lot sizing) is simplified: the strategy relies on the StockSharp `Volume` property.
- Signal evaluation occurs on finished candles only; there is no intra-bar execution.

## Usage notes
- Ensure `SignalBar` remains at least 1; the implementation maintains sufficient indicator history automatically.
- Increasing the smoothing lengths increases formation time because each stage requires the full window to complete.
- For reversal trading on higher timeframes, consider widening stop/take distances or disabling one side via permissions.

