# BykovTrend ReOpen Strategy

## Overview
The BykovTrend ReOpen strategy uses the BykovTrend logic based on Williams %R and Average True Range indicators. A buy signal occurs when the trend turns bullish and a sell signal when it turns bearish. After entering a position the strategy can re-open additional positions every predefined price step while the trend continues. Stop-loss and take-profit are applied from the last entry price.

## Indicator
The strategy does not require a separate indicator file. It calculates signals using:
- **Williams %R** with period `SSP`.
- **ATR** with fixed period 15.
Trend switches when Williams %R crosses the thresholds `-100 + K` and `-K`, where `K = 33 - Risk`.

## Trading rules
1. On a bullish signal close short positions (if allowed) and open a long position.
2. On a bearish signal close long positions (if allowed) and open a short position.
3. While a position is open, new positions in the same direction are added every `Price Step` units until `Max Positions` is reached.
4. Each position has stop-loss and take-profit distances measured from the last entry price.

## Parameters
- `Risk` – risk factor defining indicator thresholds.
- `SSP` – Williams %R period.
- `Price Step` – price distance to add a new position.
- `Max Positions` – maximum number of open positions per side.
- `Stop Loss` – stop-loss distance in price units.
- `Take Profit` – take-profit distance in price units.
- `Enable Long Open` – allow opening long positions.
- `Enable Short Open` – allow opening short positions.
- `Enable Long Close` – allow closing longs on opposite signal.
- `Enable Short Close` – allow closing shorts on opposite signal.
- `Candle Type` – timeframe used for calculations.
