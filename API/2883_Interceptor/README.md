# Interceptor Strategy (StockSharp Port)

## Overview
The Interceptor strategy is a C# port of the original MetaTrader5 expert advisor. It combines multi-timeframe EMA "fan" alignment with Stochastic oscillators, flat-range breakout detection, divergence analysis, hammer candlestick filters and horn (fan convergence) confirmation. The goal is to exploit strong trend continuation following periods of consolidation on the GBP/USD 5-minute chart.

## Core Logic
- **Trend Structure** – The strategy evaluates exponential moving averages (lengths 34/55/89/144/233) on M5, M15 and H1 timeframes. A valid trend requires all EMA fans to be aligned (ascending for bullish, descending for bearish) and the maximum distance between the slowest and fastest EMA to stay below configurable thresholds.
- **Momentum Confirmation** – M5 and M15 Stochastic oscillators must cross out of oversold/overbought areas to confirm that price is leaving congestion zones.
- **Flat Breakout Filter** – A volatility compression detector searches for tight ranges (length and width controlled by `FlatnessCoefficient`, `MinFlatBars` and `MaxFlatPoints`). Breakouts from these zones add confidence to a signal.
- **Hammer Filter** – Recent hammer or inverted hammer candles (validated via body/long shadow rules and local highs/lows) act as exhaustion signals in the direction of the intended trade.
- **Divergence Check** – The strategy looks for bullish/bearish divergences between price and the M5 Stochastic oscillator to anticipate reversals after fan alignment.
- **Horn Confirmation** – When the M5 EMA fan converges (the "horn"), a breakout above/below a recent range triggers additional entries if the higher timeframes support the move.

## Entry Conditions
A long setup may be triggered by one or multiple conditions (each adds weight to the decision):
1. EMA fans aligned on all three timeframes, M5 Stochastic bullish crossover, strong bullish candle body.
2. M5 EMA fan breakout candle that opens at the low and closes above the fast EMAs.
3. Flat-range breakout in the bullish direction.
4. M5 + M15 breakout agreement while EMA fan distances remain below the allowed thresholds.
5. Bullish divergence between Stochastic and price while fans point up.
6. Recent bullish hammer candle within the allowed lookback window.
7. M15 Stochastic bullish crossover with bullish candle bodies.
8. Horn breakout above the recent range after the EMA fan converged.

Short setups follow the mirror logic. If both long and short conditions are simultaneously present, the strategy skips trading for that bar.

## Exit & Risk Management
- Configurable fixed stop-loss and take-profit in points.
- Optional breakeven logic (`StopLossAfterBreakeven`, `TakeProfitAfterBreakeven`) that tightens the stop once price reaches a profit threshold.
- Trailing stop based on price distance from the latest close (`TrailingDistancePoints` with `TrailingStepPoints`).
- When a new position is opened, the strategy closes any existing opposite position first.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume used for each entry. |
| `FlatnessCoefficient` | Multiplier that controls the maximum allowable width of a detected flat range. |
| `StopLossPoints` | Initial stop-loss distance in price points. |
| `TakeProfitPoints` | Initial take-profit distance in price points (0 disables). |
| `TakeProfitAfterBreakeven` | Required profit (points) before breakeven logic activates. |
| `StopLossAfterBreakeven` | Distance of the breakeven stop once activated. |
| `MaxFanDistanceM5/M15/H1` | Maximum EMA spread allowed on each timeframe. |
| `StochasticKPeriodM5/M15` | %K length for Stochastic oscillators on M5 and M15. |
| `StochasticUpperM5/M15` | Overbought thresholds. |
| `StochasticLowerM5/M15` | Oversold thresholds. |
| `MinBodyPoints` | Minimum candle body size to qualify as a strong bar. |
| `MinFlatBars` | Minimum bars required to define a flat range. |
| `MaxFlatPoints` | Maximum flat-range width (points). |
| `MinDivergenceBars` | Minimum separation between divergence pivots. |
| `HammerLongShadowPercent` | Minimum long shadow percentage for hammer detection. |
| `HammerShortShadowPercent` | Maximum opposite shadow percentage for hammer detection. |
| `HammerMinSizePoints` | Minimum total range of the hammer candle. |
| `HammerLookbackBars` | Lookback window to search for hammer patterns. |
| `HammerRangeBars` | Number of bars used to validate hammer highs/lows. |
| `MaxFanWidthAtNarrowest` | Maximum EMA spread when the fan is considered converged. |
| `FanConvergedBars` | Number of bars the fan can remain converged for horn signals. |
| `RangeBreakLookback` | Lookback window for range breakout detection. |
| `TrailingStepPoints` | Minimal increment for trailing stop adjustments. |
| `TrailingDistancePoints` | Distance between price and the trailing stop. |
| `CandleType` | Primary candle series (default M5 time candles). |

## Usage Notes
- The original expert advisor was designed for GBP/USD M5 charts. Parameters may need adjustment for other instruments or timeframes.
- The strategy requires the StockSharp high-level API and candle data for M5, M15 and H1 intervals.
- Only one net position is maintained; opposite positions are closed before new trades are opened.

## Disclaimer
The strategy is provided for educational purposes. Past performance does not guarantee future results. Always validate parameters and logic in a safe test environment before trading live capital.
