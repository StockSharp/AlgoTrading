# Previous Candle Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the classic "BreakOut" MetaTrader expert by Soubra2003. It monitors the high and low of the most recent
completed candle and reacts whenever the current close breaks those reference levels. The approach is fully symmetric: long
positions are opened on bullish breakouts, and short positions are opened on bearish breakdowns. Optional stop-loss and
take-profit buffers expressed in price units allow the user to cap risk or lock in gains.

## Overview

- Subscribes to a single candle series (1-hour timeframe by default).
- Stores the previous candle's high and low to act as breakout triggers.
- Trades only at candle close to mirror the original tick-based logic without relying on intra-bar data.
- Supports both long and short trades and always stays flat when no breakout condition is active.

## Trading Rules

1. **Breakout entry / reversal**
   - When the close of the current finished candle is strictly above the previous candle's high:
     - Any open short position is closed at market.
     - A new long position is opened immediately afterward (the reversal happens within the same candle processing step).
   - When the close is strictly below the previous candle's low:
     - Any open long position is closed at market.
     - A new short position is opened afterward.
2. **Protective exits (optional)**
   - If a stop-loss offset is configured (> 0), the strategy exits a long when the close falls `offset` units below the entry
     price, or exits a short when the close rises `offset` units above the entry price.
   - If a take-profit offset is configured (> 0), the strategy exits a long when the close rises `offset` units above the entry
     price, or exits a short when the close falls `offset` units below the entry price.
3. **State reset**
   - After every candle is processed, the most recent high and low become the new breakout reference levels.

## Parameters

- **Candle Type** – data type used for subscription (defaults to hourly time frame). Set this to the bar size that matches the
  chart used in MetaTrader for the original expert.
- **Stop Loss** – distance in absolute price units between the entry price and the protective stop. Keep at `0` to disable
  stop-loss handling.
- **Take Profit** – distance in absolute price units between the entry price and the profit target. Keep at `0` to disable
  take-profit handling.

## Notes

- The stop-loss and take-profit calculations are performed on candle close prices. The original MQL4 version attached static
  SL/TP levels to the orders; in StockSharp the exits are simulated by sending market orders once the thresholds are met.
- Use instrument-specific price increments when configuring offsets. For example, if the instrument trades with 0.01 tick size
  and you want a 20-tick stop, set the stop-loss parameter to `0.20`.
- Because the logic always references the immediately preceding candle, the strategy works best on trending instruments or
  during high-volatility sessions where breakouts are meaningful.

## Origin

- **Source**: `MQL/17306/BreakOut.mq4` (BreakOut expert advisor by Soubra2003)
- **Author**: https://www.mql5.com/en/users/soubra2003
