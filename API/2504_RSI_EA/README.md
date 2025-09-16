# RSI EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The RSI EA strategy replicates the MetaTrader 5 "RSI EA" expert advisor. It watches the Relative Strength Index (RSI) on the selected candle series and reacts when momentum crosses configurable oversold or overbought levels. The conversion keeps the stop-loss, take-profit, trailing-stop, and automatic money-management ideas of the original system while adapting them to the StockSharp high-level strategy API.

## Strategy Logic

### Indicators
- **RSI** with a configurable period applied to the chosen candle type.

### Entry Conditions
- **Long**: the RSI crosses **above** `RsiBuyLevel` (previous value below the threshold, current value above the threshold) and long trading is enabled.
- **Short**: the RSI crosses **below** `RsiSellLevel` (previous value above the threshold, current value below the threshold) and short trading is enabled.

Only one net position is maintained. If the strategy is already in the market, no additional hedging positions are opened.

### Exit Conditions
- **Signal-based exit**: when `CloseBySignal` is enabled the opposite RSI crossover immediately closes the active position.
- **Protective stop**: when `StopLoss` is greater than zero the strategy monitors the price distance from the average entry price and exits once the loss reaches the specified amount.
- **Take-profit**: when `TakeProfit` is greater than zero the position is closed as soon as the target distance is reached.
- **Trailing stop**: when `TrailingStop` is greater than zero the stop level follows the price. For long positions the stop is lifted to `Close - TrailingStop` once the price advances by at least `TrailingStop` from the current stop; shorts behave symmetrically.

### Position Sizing
- When `UseAutoVolume` is `true`, the volume is calculated from account equity and risk: `Volume = Equity * RiskPercent / (100 * stopDistance)`, where `stopDistance` uses `StopLoss` if available and otherwise `TrailingStop`. If neither protective distance is set the strategy falls back to the manual volume.
- When `UseAutoVolume` is `false`, the fixed `ManualVolume` parameter is used for every order.

## Parameters
- `CandleType`: candle series used for indicator calculation (default: 1-minute time frame).
- `RsiPeriod`: number of bars in the RSI calculation window (default: 14).
- `RsiBuyLevel`: oversold boundary that triggers long entries and short exits (default: 30).
- `RsiSellLevel`: overbought boundary that triggers short entries and long exits (default: 70).
- `EnableLong`: enable or disable long trades (default: true).
- `EnableShort`: enable or disable short trades (default: true).
- `CloseBySignal`: close positions when the RSI crosses the opposite threshold (default: true).
- `StopLoss`: stop-loss distance in price units (default: 0, disabled).
- `TakeProfit`: take-profit distance in price units (default: 0, disabled).
- `TrailingStop`: trailing stop distance in price units (default: 0, disabled).
- `UseAutoVolume`: turn on risk-based position sizing (default: true).
- `RiskPercent`: percentage of equity to risk when auto sizing is active (default: 10).
- `ManualVolume`: fixed order size when auto sizing is disabled (default: 0.1).

## Implementation Notes
- The StockSharp implementation uses the high-level `SubscribeCandles(...).Bind(...)` workflow, allowing the RSI indicator to deliver values directly to the strategy without manual buffer management.
- The strategy resets all protective levels whenever the position returns to zero to avoid stale stop or take-profit values.
- Trailing logic mirrors the MQL code: the stop is only adjusted after price travels more than twice the trailing distance beyond the current stop level, preventing premature tightening.
- Because StockSharp strategies operate in a netting environment, it is not possible to hold simultaneous long and short positions as in the original hedging EA. Instead, the strategy waits for the current position to close before opening in the opposite direction.
- Automatic sizing requires either `StopLoss` or `TrailingStop` to be defined; otherwise, the manual volume is used because the risk distance is unknown.

## Default Configuration
- Time frame: 1-minute candles.
- RSI: period 14, levels 30/70.
- Money management: auto volume enabled, 10% equity risk, manual fallback volume 0.1.
- Risk controls: no stop-loss, take-profit, or trailing stop by default (must be configured for live trading).

## Usage Tips
- Set `CandleType` to match the instrument and time horizon you intend to trade; the strategy works on any interval supported by StockSharp candles.
- Provide realistic stop-loss or trailing-stop distances before enabling auto sizing so that the risk calculation uses meaningful values.
- Combine the strategy with `StartProtection()` (already called in the code) to let the framework manage unexpected disconnections or orphaned positions.
- Monitor fills and adjust the RSI levels when applying the strategy to different markets, as optimal thresholds can vary.
