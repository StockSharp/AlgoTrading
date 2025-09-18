# Fuzzy Logic Legacy Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy reproduces the 2007 "Fuzzy logic" MetaTrader expert advisor in StockSharp. It combines several Bill Williams tools
with momentum oscillators and evaluates them through a fuzzy scoring table. Only when the aggregated score shows strong bullish o
r bearish consensus does the system open a new position. A fixed stop-loss and optional trailing stop mirror the original trade m
anagement rules.

## Trading Logic

1. Build the Bill Williams Alligator (jaw, teeth, lips) using smoothed moving averages and calculate the *Gator* spread as the su
m of absolute distances between the lines.
2. Calculate Williams %R (period 14), DeMarker (period 14) and RSI (period 14) on the same candles.
3. Derive the Accelerator Oscillator (AC) from the Awesome Oscillator sequence and track up to five consecutive bars to detect ac
celeration streaks.
4. Each indicator feeds a five-level fuzzy membership table with predefined breakpoints copied from the original code.
5. Weighted sums of the memberships produce a decision value between 0 and 1:
   - Values **> 0.75** indicate bullish consensus and trigger long entries.
   - Values **< 0.25** indicate bearish consensus and trigger short entries.
6. Only one position can be open at a time. Protective stops are attached immediately after entry.

## Position Management

- **Stop-loss**: Fixed distance in price steps (`Stop Loss (points)` parameter).
- **Trailing stop**: Optional; when enabled it trails the protective stop by the specified number of price steps.
- **Money management**: Optional balance-based sizing that mimics the MetaTrader formula `Volume = (Balance * (PercentMM + DeltaM
M) - InitialBalance * DeltaMM) / 10000`.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Candle Type` | Candle data series used for analysis. |
| `Long Threshold` | Decision level that must be exceeded to open a long position. |
| `Short Threshold` | Decision level that must be crossed to open a short position. |
| `Stop Loss (points)` | Distance of the initial stop-loss in price steps. |
| `Trailing Stop (points)` | Distance of the trailing stop in price steps; set to `0` to disable. |
| `Fixed Volume` | Trade volume when money management is disabled. |
| `Use Money Management` | Enables the MetaTrader-style money management formula. |
| `Percent MM` | Percentage of the account balance used in the money management formula. |
| `Delta MM` | Additional percentage offset for the money management formula. |
| `Initial Balance` | Reference balance used by the money management formula. |

## Notes

- The strategy uses only completed candles (`CandleStates.Finished`) to avoid repainting.
- All indicator levels and weights follow the original expert advisor, preserving its behaviour.
- To run the system intraday, adjust the candle timeframe and thresholds to reflect the desired volatility.
