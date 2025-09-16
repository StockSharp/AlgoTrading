# MACD EA Strategy

This strategy is a StockSharp port of the MetaTrader 5 expert advisor `MACD EA (barabashkakvn's edition).mq5` from folder `MQL/20010`. It recreates the same MACD crossover logic, partial profit taking, and money-management features while using the high-level StockSharp API.

## Trading logic

* **Signal source** – A classic MACD indicator is calculated with configurable fast, slow, and signal periods. The strategy examines the difference between the MACD line and the signal line two and four completed candles ago. A bullish crossover (difference turns from negative to positive) opens a long trade, while the opposite condition opens a short trade.
* **Position management** – Every order is protected by configurable stop-loss and take-profit offsets measured in pips. The offsets are converted to prices by using the instrument price step and multiplying by ten when the instrument has 3 or 5 decimal places, mimicking the original EA's point adjustment.
* **Partial profit** – When enabled, half of the open position is closed once price travels `PartialProfitPips` in the trade direction. The remaining portion keeps running.
* **Breakeven** – After price advances `BreakevenPips` in favor, the strategy enables a breakeven guard. If price returns to the original entry level, the position is closed at the entry price, just like the EA moves the stop to breakeven.
* **Opposite MACD signal** – An opposite MACD crossover closes any remaining exposure immediately, ensuring that the strategy never keeps a position against the indicator trend.

## Money management

When `UseMoneyManagement` is enabled, the position size increases after consecutive losing trades. The next trade uses a multiplier based on the number of consecutive losses (x2 after one loss, x3 after two losses, up to x7 for six or more losses). The multiplier is combined with the `RiskMultiplier` parameter to reproduce the martingale-style sizing of the original code. Winning trades reset the loss counter to zero.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | MACD calculation lengths.
| `StopLossPips` | Distance to the protective stop in pips (0 disables it).
| `TakeProfitPips` | Distance to the profit target in pips (0 disables it).
| `PartialProfitPips` | Pips needed to close half of the position (0 disables partial exit).
| `BreakevenPips` | Pips required before breakeven mode is armed (0 disables breakeven).
| `UseMoneyManagement` | Enables dynamic position sizing based on the loss streak.
| `RiskMultiplier` | Additional multiplier applied when money management is active.
| `BaseVolume` | Base trade volume before any scaling.
| `CandleType` | Candle series used for indicator calculations.

## Notes

* The strategy uses `SubscribeCandles` and indicator binding to follow the recommended high-level API pattern.
* A separate Python version is not yet available. Only the C# implementation in the `CS` folder is provided.
* Tests were not added or modified as requested.
