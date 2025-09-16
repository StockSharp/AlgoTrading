# Line Order Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is an adaptation of the MetaTrader expert advisor "MyLineOrder" for the StockSharp API. It allows a trader to define horizontal price levels that trigger automatic market orders when touched. Optional stop loss, take profit and trailing stop distances are expressed in pips, and the trade volume can be configured.

When the market price reaches the **BuyPrice** level, the strategy enters a long position. Hitting the **SellPrice** level opens a short position. After entry, the strategy monitors the position and exits when one of the protective conditions is met: stop loss, take profit or trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: Price touches or exceeds `BuyPrice`.
  - **Short**: Price touches or falls below `SellPrice`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Stop loss, take profit or trailing stop.
- **Stops**:
  - `StopLossPips`, `TakeProfitPips`, `TrailingStopPips`.
- **Filters**:
  - None.
- **Parameters**:
  - `BuyPrice` – level for long entry.
  - `SellPrice` – level for short entry.
  - `StopLossPips` – stop-loss distance in pips.
  - `TakeProfitPips` – take-profit distance in pips.
  - `TrailingStopPips` – trailing stop distance in pips.
  - `TradeVolume` – order volume.
  - `CandleType` – timeframe of candles for price monitoring.
