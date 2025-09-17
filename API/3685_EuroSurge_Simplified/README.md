# EuroSurge Simplified Strategy

## Overview
- Converts the MetaTrader 4 expert advisor **"EuroSurge Simplified"** to the StockSharp high-level API.
- Trades on finished candles and evaluates a collection of classic indicators (MA, RSI, MACD, Bollinger Bands, Stochastic) to find entries.
- Enforces a configurable cool-down period between trades and attaches take-profit / stop-loss levels expressed in price steps.
- Supports multiple position sizing modes: fixed volume, balance percentage, and equity percentage.

## Signals
1. **Moving average trend** (optional): a fast 20-period SMA must be above (long) or below (short) a slower configurable SMA.
2. **RSI filter** (optional): RSI must stay below the long threshold to allow buys and above the short threshold to allow sells.
3. **MACD confirmation** (optional): MACD line must be greater than (long) or less than (short) the signal line.
4. **Bollinger Bands filter** (optional): price must breach the lower band for longs or the upper band for shorts.
5. **Stochastic filter** (optional): %K and %D both need to remain below 50 for longs or above 50 for shorts.

All enabled filters must agree before the strategy submits a market order. Opposite exposure is flattened before opening a new position, mirroring the MetaTrader logic of replacing open trades.

## Risk Management
- Stop-loss and take-profit distances are defined in price steps (MetaTrader “points”).
- The strategy automatically registers protective orders with `SetStopLoss` and `SetTakeProfit` right after opening a position.
- Trades are blocked until the configured interval in minutes has elapsed since the last filled order.

## Position Sizing
- **FixedSize**: trades with the configured `FixedVolume`.
- **BalancePercent**: allocates a fraction of the portfolio starting balance and approximates volume by dividing by the latest close price.
- **EquityPercent**: behaves the same but relies on the current portfolio equity.
- Volumes are snapped to the security volume step and clamped between the exchange’s min/max limits.

## Parameters
| Name | Description |
| ---- | ----------- |
| `TradeSizeType` | Position sizing mode (fixed, balance %, equity %).
| `FixedVolume` | Volume used when `TradeSizeType = FixedSize`.
| `TradeSizePercent` | Percentage applied in percent-based sizing.
| `TakeProfitPoints` / `StopLossPoints` | Protective distances in price steps.
| `MinTradeIntervalMinutes` | Cool-down between trades.
| `MaPeriod` | Slow SMA length (fast SMA is fixed at 20 in line with the EA).
| `RsiPeriod`, `RsiBuyLevel`, `RsiSellLevel` | RSI configuration and thresholds.
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD parameters.
| `BollingerLength`, `BollingerWidth` | Bollinger Bands settings.
| `StochasticLength`, `StochasticK`, `StochasticD` | Stochastic oscillator parameters.
| `UseMa`, `UseRsi`, `UseMacd`, `UseBollinger`, `UseStochastic` | Toggle individual filters.
| `CandleType` | Timeframe used for signal evaluation.

## MetaTrader Differences
- The original EA validates volume against broker-specific constraints. The port mirrors this by snapping to StockSharp volume steps and honoring min/max volume when available.
- Protective levels are converted to price steps via StockSharp helpers instead of manual price arithmetic.
- All indicator values are consumed through the high-level binding API without direct calls to `GetValue`.

## Usage Tips
1. Attach the strategy to a portfolio and security, then configure the timeframe via `CandleType`.
2. Adjust the indicator toggles to reproduce or simplify the original EA behavior.
3. Increase `MinTradeIntervalMinutes` if you need fewer trades; decrease it for more frequent entries.
4. Verify that `TakeProfitPoints` and `StopLossPoints` match the symbol’s tick size.
