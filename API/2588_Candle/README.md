# Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The **Candle Strategy** is a direct port of the classic MT5 expert "Candle.mq5". It evaluates the color of every finished candle on the selected timeframe and keeps the position aligned with the most recent close. Bullish candles drive the strategy long, bearish candles drive it short, and flat candles leave the position untouched. Risk is controlled by pip-based take-profit and trailing-stop distances that are converted to absolute prices through the instrument tick size.

The strategy only reacts after a candle is fully formed to avoid mid-bar noise. A mandatory lookback (`MinBars * 2` finished candles) validates that the chart contains sufficient history, while a configurable cooldown waits between trade operations. This produces a faithful StockSharp implementation of the original MetaTrader logic without relying on low-level series access.

## Trading Logic
### Preparation
- Process candles provided by `CandleType`; no other data sources are required.
- Wait until at least `2 * MinBars` finished candles have been processed before allowing entries.
- Trade only when the strategy is online, formed, and allowed to execute orders.
- Enforce the `TradeCooldown` interval (default 10 seconds) between any two trade operations.

### Entry and Reversal Rules
1. **Flat state:**
   - Enter long (`BuyMarket`) when a candle closes above its open.
   - Enter short (`SellMarket`) when a candle closes below its open.
2. **Existing position:**
   - If a long position faces a bearish candle, sell `|Position| + Volume` to close and immediately reverse to a short position of size `Volume`.
   - If a short position faces a bullish candle, buy `|Position| + Volume` to close and immediately reverse to a long position of size `Volume`.
3. **Neutral candles:**
   - When the close equals the open, no manual action is taken; only the protective orders may exit the trade.

### Risk Management and Exits
- `StartProtection` attaches a take-profit and trailing stop measured in pips. The strategy multiplies each pip value by `(PriceStep * 10)` to match the MetaTrader adjustment for 3- and 5-digit quotes.
- The trailing stop is activated only when `TrailingStopPips` is greater than zero; it follows price automatically once the trade moves in the favorable direction.
- The take-profit closes the position when the configured distance is reached. Either protective level cancels the opposite order after execution.
- Manual reversals caused by candle color also flatten the previous exposure before opening the new position.

## Parameters
- `CandleType` – timeframe of the candle series to analyze (default: 15-minute candles).
- `TakeProfitPips` – distance to the take-profit target in pips (default: 50).
- `TrailingStopPips` – trailing stop distance in pips (default: 30).
- `MinBars` – minimum bar count required before the first trade (default: 26; strategy waits for 52 finished candles).
- `TradeCooldown` – waiting period after any trade action (default: 10 seconds).

Set the strategy `Volume` property to the desired order size. When the market reverses, the strategy automatically submits enough volume to both exit the previous position and establish the new one.

## Implementation Notes
- Only finished candles (`CandleStates.Finished`) are processed. This mirrors the MetaTrader expert, which relied on closed bar values obtained via `CopyOpen/CopyClose`.
- The code uses StockSharp's high-level API: `SubscribeCandles` for data, `Bind` to process incoming bars, and `BuyMarket`/`SellMarket` for order execution.
- Protective orders are managed by `StartProtection`, so no manual stop-limit order bookkeeping is necessary.
- The pip size computation `PriceStep * 10` reproduces the MQL "digits adjust" logic for symbols quoted with 3 or 5 decimal places.
- Because entries are triggered by the most recent candle body, the strategy tends to stay in the market continuously, alternating sides whenever candle color flips.

Adjust the pip distances, cooldown, and timeframe to match the instrument being traded. The default configuration mirrors the original MT5 sample but can be optimized through StockSharp's parameter framework.
