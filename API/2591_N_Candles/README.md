# N Candles Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The N Candles strategy replicates the MQL expert advisor that enters a trade when a configurable number of consecutive candles share the same direction. Once the most recent `N` finished candles are all bullish, the strategy sends a market buy order. When all are bearish, it sends a market sell order. No exit logic is included; the position must be managed externally or by additional strategies.

## Overview

- **Market Regime**: Works best in markets that exhibit short momentum bursts.
- **Instruments**: Any instrument supporting continuous trading (FX, futures, crypto).
- **Timeframes**: Configurable; default is 1-hour candles.
- **Order Types**: Market orders without protective stops or targets.

## How It Works

1. On every finished candle the strategy evaluates the last `N` candles.
2. If every candle in that window is bullish, it issues a buy market order with the configured volume.
3. If every candle is bearish, it issues a sell market order.
4. Doji candles (equal open and close) reset the count and suppress trading until a new streak forms.
5. The strategy does not manage open positions; repeated signals add to the existing direction on netting accounts.

## Parameters

- **Consecutive Candles**: Number of identical candles required before placing an order.
- **Volume**: Market order size sent on each signal.
- **Candle Type**: Candle series used for streak detection (timeframe or custom candle type).

## Usage Notes

- Because the strategy lacks stops or exits, combine it with manual management, protective strategies, or portfolio risk controls.
- On highly volatile markets consider lowering the candle count or timeframe to capture faster streaks.
- Excessive consecutive streaks can accumulate large positions; monitor leverage and account limits.
