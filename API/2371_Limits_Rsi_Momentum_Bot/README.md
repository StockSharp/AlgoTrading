# Limits RSI Momentum Bot Strategy

## Summary
This strategy places limit orders based on Relative Strength Index (RSI) and Momentum indicators. It aims to buy at discounts and sell at premiums by using pending orders instead of market executions.

## Trading Rules
- Operates only during the specified time window.
- On each finished candle, RSI and Momentum values are calculated.
- **Buy limit** is placed below the candle open when RSI and Momentum are both below their buy thresholds.
- **Sell limit** is placed above the candle open when RSI and Momentum are both above their sell thresholds.
- When a position is opened, the opposite pending order is cancelled.
- Stop-loss and take-profit are managed automatically via `StartProtection`.

## Parameters
- `Volume` – order volume.
- `LimitOrderDistance` – distance in price steps from the candle open to place pending orders.
- `TakeProfit` – profit target in price steps.
- `StopLoss` – loss limit in price steps.
- `RsiPeriod` – period for RSI calculation.
- `RsiBuyRestrict` / `RsiSellRestrict` – RSI thresholds that allow long or short entries.
- `MomentumPeriod` – period for Momentum calculation.
- `MomentumBuyRestrict` / `MomentumSellRestrict` – Momentum thresholds for long or short entries.
- `StartTime` / `EndTime` – trading session boundaries.
- `CandleType` – candle interval used for indicator calculations.

## Notes
The strategy is converted from the MQL4 script "The Limits Bot with RSI & Momentum" and uses the high-level API of StockSharp.
