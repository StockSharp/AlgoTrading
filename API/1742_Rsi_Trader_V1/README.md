# RSI Trader V1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Relative Strength Index (RSI) to identify reversals after short-term extremes. A buy signal occurs when RSI crosses above the oversold threshold after staying below it for two consecutive candles. A sell signal occurs when RSI crosses below the overbought threshold after staying above it for two candles. The strategy optionally closes an existing opposite position and trades only within a configurable time window.

## Details

- **Entry Criteria**:
  - **Long**: `RSI > BuyPoint` and RSI for the previous two candles `< BuyPoint`.
  - **Short**: `RSI < SellPoint` and RSI for the previous two candles `> SellPoint`.
- **Exit Criteria**: Opposite signal or protective stop/take-profit.
- **Time Filter**: Trades only when the candle's opening hour is between `StartHour` and `EndHour`.
- **Stops**: Fixed take profit and stop loss expressed in price units.
- **Parameters**:
  - `RsiPeriod` – RSI calculation period.
  - `BuyPoint` – oversold level for long entries.
  - `SellPoint` – overbought level for short entries.
  - `CloseOnOpposite` – close current position when opposite signal appears.
  - `StartHour` / `EndHour` – trading hours.
  - `TakeProfit` / `StopLoss` – protective levels in price.

This example demonstrates a minimalistic RSI crossover system built with the high-level StockSharp API. It can be used as a template for further experimentation.
