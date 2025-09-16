# Flat Trend EA Strategy

## Overview
Flat Trend EA Strategy is a StockSharp port of the MQL5 expert advisor "Flat Trend EA". The algorithm combines the Parabolic SAR indicator with the Average Directional Index (ADX) to detect four market states: trending up, trending down, end of buy, and end of sell. The strategy reacts only to completed candles from the configured timeframe and mirrors the original logic of closing opposite positions before opening a new one.

## Trading Logic
- **Buy signal**: the Parabolic SAR dot prints below the close price and the ADX +DI line is above the -DI line. Any short exposure is closed immediately, and a new long is opened when no position is active.
- **Sell signal**: the Parabolic SAR dot prints above the close price and +DI is less than or equal to -DI. Any long exposure is closed before opening a short trade.
- **End-of-trend filters**: when the SAR is above price while +DI is greater than -DI the strategy marks the end of a short trend; when SAR is below price while +DI is less than or equal to -DI it marks the end of a long trend. Both events force existing positions to be closed without opening a new trade.
- **Trading window**: optional session filters restrict entries to the `[StartHour, EndHour)` interval. Signals outside the session can still close trades, but new entries are skipped.

## Risk Management
- **Stop-loss and take-profit** distances are measured in pips (automatically scaled for three- and five-digit instruments). Prices are normalized to the security step.
- **Trailing stop** activates after the position gains more than `TrailingStopPips + TrailingStepPips`. Long positions trail below the latest close, shorts trail above. When trailing is disabled the stop level remains fixed.
- **Protective exits**: on every finished candle the strategy checks low/high prices against stop-loss, take-profit, and trailing levels. Any breach closes the position and resets risk tracking.

## Parameters
- `StopLossPips` – distance to the protective stop in pips.
- `TakeProfitPips` – distance to the target in pips.
- `TrailingStopPips` – trailing stop distance in pips (set to 0 to disable trailing).
- `TrailingStepPips` – extra progress required before the trailing stop moves; must be positive when trailing is enabled.
- `UseTradingHours` – enables the trading window filter.
- `StartHour` / `EndHour` – inclusive start hour and exclusive end hour for entries (exchange time).
- `AdxPeriod` – smoothing period for ADX, which controls +DI and -DI sensitivity.
- `SarStart`, `SarIncrement`, `SarMaximum` – Parabolic SAR acceleration settings matching the original indicator (0.02 / 0.02 / 0.2 by default).
- `CandleType` – timeframe used for candle subscriptions and indicator calculations.
- `Volume` – inherited from `Strategy`, represents the order size used when entering new positions.

## Indicators
- **Average Directional Index (ADX)** provides the +DI and -DI components used to determine the current trend direction.
- **Parabolic SAR** defines whether the market structure is bullish or bearish and supplies the dot level for trailing logic.

## Additional Notes
- Pip size is computed from the security settings: for three- and five-decimal instruments the price step is multiplied by ten to match the MQL definition of a pip.
- The strategy always closes existing positions when opposite or end signals appear before evaluating new entries, reproducing the original EA workflow.
- Only the C# implementation is supplied; no Python version or folder is created, as requested.
