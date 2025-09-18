# Dig Variation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is inspired by the MQL5 *DigVariation* example. It approximates the indicator using a simple moving average (SMA) and opens trades when the SMA trend changes direction.

## Logic
- The SMA is calculated on incoming candles.
- If the previous SMA values show an upward slope and the latest value continues higher, the strategy opens a long position.
- If the previous SMA values show a downward slope and the latest value continues lower, the strategy opens a short position.
- Existing positions are closed when the trend reverses.

## Parameters
- **Period** – SMA calculation period.
- **BuyOpen** – enable long entries.
- **SellOpen** – enable short entries.
- **BuyClose** – allow closing long positions.
- **SellClose** – allow closing short positions.
- **StopLoss** – loss protection value (passed to `StartProtection`).
- **TakeProfit** – profit target value (passed to `StartProtection`).

## Notes
This is a simplified conversion. It uses a standard SMA instead of the original custom DigVariation indicator.

