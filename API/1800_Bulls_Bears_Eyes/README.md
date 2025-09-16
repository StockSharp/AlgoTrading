# Bulls Bears Eyes Strategy

## Overview
This strategy evaluates the balance between bullish and bearish pressure using the **Bulls Power** and **Bears Power** indicators. The two indicators are combined into a single oscillator scaled from 0 to 100. High values indicate dominance of buyers, while low values point to sellers' strength.

Trading decisions are based on threshold levels similar to the original *BullsBearsEyes* expert. When the oscillator crosses above the overbought level after being below it, a long position is opened and any short position is closed. Conversely, crossing below the oversold level triggers a short entry and closes existing longs. Neutral values between the thresholds keep the current position but close opposing trades.

## Parameters
- **Period** – averaging period for Bulls/Bears Power (default: 13).
- **High Level** – overbought threshold generating long signals (default: 75).
- **Middle Level** – reference middle level used for trend interpretation (default: 50).
- **Low Level** – oversold threshold generating short signals (default: 25).
- **Candle Type** – timeframe of candles processed by the strategy (default: 4‑hour candles).

## Entry and Exit Rules
1. Calculate Bulls Power and Bears Power for each candle and derive the oscillator value between 0 and 100.
2. **Long entry**: oscillator crosses above *High Level* after being below it. Any short position is closed before opening the long.
3. **Short entry**: oscillator crosses below *Low Level* after being above it. Any existing long position is closed before opening the short.
4. **Position exit**: when the oscillator switches sides (above/below the middle zone), the opposite position is closed.

The oscillator is also plotted together with the candles for visual analysis.

## Notes
- The strategy uses high‑level `SubscribeCandles` and `Bind` API for indicator processing.
- Protective mechanisms are enabled via `StartProtection()` at start.
- Only completed candles are evaluated to avoid premature signals.
