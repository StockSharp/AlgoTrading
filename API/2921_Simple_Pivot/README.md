# Simple Pivot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the "SimplePivot" MetaTrader 5 expert advisor. It continuously evaluates the relationship between the
current bar open and the prior bar's pivot level, always maintaining a single directional position. When the bias flips, the
strategy closes the existing position and immediately opens one in the opposite direction.

## Overview

- **Market regime**: Always-in-the-market swing trading.
- **Instruments**: Any instrument that provides candle data for the selected timeframe.
- **Timeframes**: Configurable through the *Candle Type* parameter (default 1-hour candles).
- **Orders**: Market orders sized by the *Volume* parameter.

## How It Works

### Pivot calculation

1. Wait for at least one completed candle to seed the calculation.
2. Compute the pivot of the previous candle as the arithmetic mean of its high and low prices.
3. Retain the previous high and low so the pivot for the next bar can be produced immediately when a new candle finishes.

### Directional decision

1. Default bias is long (buy).
2. If the current candle opens below the previous high while staying above the pivot, the bias switches to short (sell).
3. If the desired direction is unchanged from the last executed trade, the existing position is preserved and no new orders are
   sent.

### Position management

1. If the desired direction differs from the current trade, the running position is flattened via an opposite market order.
2. After flatting, a market order sized by *Volume* establishes the new directional exposure.
3. The process repeats on each finished candle, ensuring the strategy is always either long or short.

## Parameters

- **Volume**: Trade size used for every entry. It also determines the size of the closing order when the strategy flips
  direction.
- **Candle Type**: Data type of the candles used for pivot and entry calculations. The default is a 1-hour time frame but any
  available time frame can be selected.

## Additional Notes

- The logic reacts on fully completed candles (`CandleStates.Finished`) to avoid repeated signals while a candle is still
  forming.
- No stops or profit targets are defined; exits occur only when the pivot rule requests a direction change.
- Because the strategy is always in the market, risk controls such as max drawdown monitoring or session filters should be
  handled externally if required.
