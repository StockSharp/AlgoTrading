# Random Coin Toss Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the classic GuruTrader example where trade direction is determined by a coin toss.
On each finished candle, if no position is open, a pseudo random number is generated and treated as a coin flip.
Heads opens a long position while tails opens a short one.
Every trade applies fixed take-profit and stop-loss distances measured in absolute price units.

## Parameters
- **Take Profit** – distance from entry price to place the take-profit order.
- **Stop Loss** – distance from entry price to place the stop-loss order.
- **Use Time Seed** – seed the random generator with current time for different results on each run. When disabled, a fixed seed is used.
- **Candle Type** – type of candles processed by the strategy.

## Trading Logic
1. Wait for a finished candle.
2. Ensure the strategy is allowed to trade and no position is open.
3. Generate a random value and choose direction based on the coin toss.
4. Protect the position with the predefined stop-loss and take-profit distances.

**Warning:** This strategy is for educational purposes only and should never be used on live accounts.
