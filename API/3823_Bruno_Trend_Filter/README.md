# Bruno Trend Strategy

## Overview

Bruno Trend Strategy is a StockSharp port of the MetaTrader expert advisor "Bruno_v1". The strategy trades on 30-minute candles and focuses on synchronized bullish signals from several classic trend-following and momentum indicators. Only long positions are opened, mimicking the original expert that concentrated on bullish breakouts confirmed by indicator alignment.

## Trading Logic

1. **Timeframe**: 30-minute candles.
2. **Indicators**:
   - Simple Moving Average (SMA) with length 4 used as a short-term momentum gauge.
   - Exponential Moving Averages (EMAs) with lengths 8 and 21 to define the primary trend direction.
   - Average Directional Index (ADX) with period 13 to ensure directional strength via +DI and -DI components.
   - Stochastic Oscillator with parameters %K=21, %D=3, slowing=3 to confirm momentum while avoiding overbought levels.
   - MACD (13, 34, 8) for histogram and signal line confirmation.
   - Parabolic SAR (step 0.055, maximum 0.21) to verify upward acceleration and manage exits.
3. **Entry Rules**:
   - EMA(8) must be above EMA(21).
   - ADX filter: +DI greater than -DI and above 20.
   - Stochastic filter: %K above %D but still below 80 to stay out of overbought extremes.
   - MACD histogram above zero and above the signal line.
   - Parabolic SAR rising (current SAR higher than the previous reading).
   - Current position must be flat or short. Any short position is closed before entering the new long trade.
4. **Exit Rules**:
   - Close the long position when the previous candle close falls below the previous Parabolic SAR value, replicating the MetaTrader exit trigger.

## Risk Management

- Default lot size: 0.1 lots.
- Optional MetaTrader-style protection: 50 pip take-profit and 30 pip stop-loss, configured with `StartProtection`. Trailing stops are disabled by default to mirror the original script.

## Notes

- The strategy ignores the unused short setup from the MetaTrader code, matching the original behavior where short trades were effectively disabled.
- Indicator values are processed via StockSharp's high-level API to avoid manual buffering and stay aligned with the project guidelines.
