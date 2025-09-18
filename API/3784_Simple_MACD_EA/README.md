# Simple MACD EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The Simple MACD EA strategy is a direct port of the classic MetaTrader expert advisor "Simple MACD EA". The approach uses two exponential moving averages (EMAs) to emulate the MACD histogram and determine the dominant trend on one-minute candles. Long positions are opened when the fast EMA (period 100) crosses above the slow EMA (user-defined MACD level). Short positions are opened when the fast EMA drops below the slow EMA. Only one position is maintained at any time.

## Trade Management Logic
- **Trend detection:** The difference between the 100-period EMA and the configurable MACD EMA defines the current trend direction (`+1`, `0`, `-1`). A reversal from negative to positive opens a long position. A reversal from positive to negative opens a short position.
- **Momentum confirmation:** The strategy keeps track of the difference between the MACD EMA and a slightly slower EMA (`MACD level + 1`). If the difference shrinks against the current trade after the price has moved at least five points in profit, the position is closed early.
- **Time-based protection:** After a trade stays open for a user-defined number of evaluation cycles, the system activates a soft stop that reduces tolerance for adverse price movement relative to the entry price.
- **Trailing exit:** Once the trade moves into profit and stays active for enough cycles, an internal trailing stop is engaged. The stop level follows the price by the configured number of points and can be updated a limited number of times. If the limit is reached the position is closed.
- **Trend reversal exit:** When the trend signal flips in the opposite direction while price is already five points in profit, the position is closed immediately.

## Parameters
- **Candle Type** – Time frame used for the EMA calculations (default: 1-minute candles).
- **Volume** – Order volume for new entries.
- **MACD Level** – EMA length that defines the slow MACD component. A secondary EMA with length `MACD Level + 1` is derived automatically.
- **Trailing Stop** – Distance in points for the trailing exit. Set to zero to disable.
- **Trailing Updates** – Maximum number of trailing stop adjustments per trade.
- **Wait Cycles** – Number of candle evaluations to wait before the adaptive soft stop becomes active.

## Additional Notes
- The strategy always flattens the current position before reversing direction.
- Price step information from the selected security is used to translate point-based distances into actual prices.
- The implementation relies on StockSharp's high-level candle subscription API and does not enqueue custom indicator buffers.
