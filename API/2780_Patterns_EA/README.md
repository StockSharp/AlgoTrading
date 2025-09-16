# Patterns EA Strategy

## Overview
Patterns EA Strategy is a price action system that scans the most recent three finished candles for a wide range of single, double, and triple-bar formations. The logic is a StockSharp port of the original MQL5 "Patterns_EA" expert advisor and preserves its configurable catalogue of 30 candlestick setups. Each pattern can be enabled or disabled independently and can be assigned to either long or short execution, allowing the strategy to mimic the discretionary rules from the source script.

## Pattern Groups
The detector evaluates the current candle and up to the two previous candles depending on the pattern group:

- **Group 1 – One-bar patterns:** Neutral Bar, Force Bar Up, Force Bar Down, Hammer, Shooting Star.
- **Group 2 – Two-bar patterns:** Inside, Outside, Double Bar High Lower Close, Double Bar Low Higher Close, Mirror Bar, Bearish Harami, Bearish Harami Cross, Bullish Harami, Bullish Harami Cross, Dark Cloud Cover, Doji Star, Engulfing Bearish Line, Engulfing Bullish Line, Two Neutral Bars.
- **Group 3 – Three-bar patterns:** Double Inside, Pin Up, Pin Down, Pivot Point Reversal Up, Pivot Point Reversal Down, Close Price Reversal Up, Close Price Reversal Down, Evening Star, Morning Star, Evening Doji Star, Morning Doji Star.

A tolerance parameter (Equality Pips) controls how closely two prices must match to satisfy equality checks, reproducing the "maximum pips distance" setting of the original EA.

## Parameters
- **Candle Type** – Time frame used for pattern detection.
- **Opened Mode** – Position management logic (Any, Swing, Buy One, Buy Many, Sell One, Sell Many) replicated from the MQL version.
- **Equality Pips** – Pip distance that defines price equality; adjusted by the instrument's price step.
- **Enable One-Bar Patterns / Enable Two-Bar Patterns / Enable Three-Bar Patterns** – Toggles for each pattern group.
- **Enable {Pattern}** – Individual switches for all 30 formations.
- **{Pattern} Order** – Trade direction (buy or sell) assigned to the corresponding pattern.

All parameters are exposed through `StrategyParam` objects, enabling optimisation or UI binding when used within StockSharp applications.

## Trading Logic
1. The strategy subscribes to the configured candle series and waits for finished candles.
2. When a new bar closes, the latest three candles are cached, and the detector evaluates the enabled pattern groups.
3. Each pattern replicates the conditional rules from the MQL5 source, including tolerant comparisons and shadow/body relationships.
4. When a pattern is confirmed, `TriggerPattern` checks whether the group and the individual pattern are enabled, verifies the selected direction, and applies the configured position mode.
5. The strategy sends a market order in the assigned direction. In Swing mode it first closes any open position in the opposite direction.

## Position Modes
- **Any:** Allows signals in both directions without additional constraints.
- **Swing:** Maintains a single net position; opposite signals flatten the existing position before entering the new one.
- **Buy One / Sell One:** Restrict trades to a single long or short position respectively.
- **Buy Many / Sell Many:** Allow multiple entries in the specified direction while ignoring signals in the opposite direction.

## Notes
- The algorithm uses `Security.PriceStep` to translate the equality tolerance into absolute price distance. If the instrument does not define a price step, a default of 0.0001 is applied.
- No additional indicators are required; all logic relies solely on candle geometry, matching the intent of the original expert advisor.
