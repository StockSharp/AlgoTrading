# Swing Cyborg Strategy

## Overview
Swing Cyborg is a discretionary helper that automates execution based on a trader's own trend forecast. The user defines the expected trend direction and the time window when it should be valid. The strategy then confirms entries with the RSI indicator and manages exits with fixed targets.

## Parameters
- `Volume` – order volume in lots.
- `TrendPrediction` – expected trend direction (Uptrend or Downtrend).
- `TrendTimeframe` – timeframe used for RSI and trading (M30, H1 or H4).
- `TrendStart` – beginning of the user defined trend period.
- `TrendEnd` – end of the user defined trend period.
- `Aggressiveness` – money management preset:
  - Low: take profit 300 pips, stop loss 200 pips.
  - Medium: take profit 500 pips, stop loss 250 pips.
  - High: take profit 600 pips, stop loss 300 pips.

## Trading Logic
1. Wait for a new candle on the selected timeframe.
2. Trade only if current time is between `TrendStart` and `TrendEnd`.
3. Calculate RSI(14).
4. If there is no open position:
   - If `TrendPrediction` is Uptrend and RSI ≤ 65 → buy.
   - If `TrendPrediction` is Downtrend and RSI ≥ 35 → sell.
5. `StartProtection` automatically closes the position when profit or loss reaches the predefined level.

The strategy operates on finished candles and does not open a new position while an existing one is active.
