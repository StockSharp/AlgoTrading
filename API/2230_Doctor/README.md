# Doctor Strategy

Implementation of strategy #15233 "Doctor" converted from MQL to StockSharp.

## Overview
The strategy combines several classic indicators to detect trend direction and momentum:

- **Slope detection** using a 40-period Weighted Moving Average to evaluate trend direction.
- **Linear location** via a 400-period Weighted Moving Average compared against the highs and lows of the last three candles.
- **Momentum** confirmation with Relative Strength Index periods 14 and 5.
- **Trend reversal** filter from Parabolic SAR.

A long position is opened when all bullish conditions align, and a short position when all bearish conditions align. Existing positions are closed on opposite signals or when protective levels are hit. An optional trailing stop pushes the stop loss forward once half of the stop distance is achieved.

## Parameters
- `StopLossTicks` – stop-loss distance in ticks.
- `TakeProfitTicks` – take-profit distance in ticks.
- `TrailingStop` – enables trailing stop logic.
- `CandleType` – timeframe used for candles (default 30 minutes).

## Trading rules
1. **Buy** when:
   - WMA(40) slope is rising.
   - WMA(400) is above the highs of the last three candles.
   - RSI(14) is above 50 and RSI(5) is below RSI(14).
   - No open long position.
2. **Sell** when:
   - WMA(40) slope is falling.
   - WMA(400) is below the lows of the last three candles.
   - RSI(14) is below 50 and RSI(5) is above RSI(14).
   - No open short position.
3. **Exit** when the opposite conditions occur or stop-loss/take-profit levels are reached. The trailing stop updates the stop level after price moves half of the stop distance in favor.

## Indicators
- Weighted Moving Average (40, 400)
- Relative Strength Index (14, 5)
- Parabolic SAR
