# Expert Ichimoku Strategy

## Overview

The Expert Ichimoku strategy replicates the logic of the original MQL5 "Expert Ichimoku" expert advisor using StockSharp's high-level API. The system is a directional trend-following model that combines multiple components of the Ichimoku Kinko Hyo indicator with price-action filters and an optional martingale-style position sizing module.

The strategy evaluates signals on completed candles of a configurable timeframe. Long and short trades are mutually exclusive — the strategy maintains a single net position and flips direction only after closing the existing exposure. All indicator values are calculated on the subscribed candle series; no external data is required.

## Core Logic

### Indicator Configuration

* **Tenkan-sen (Conversion Line):** Fast moving average used for crossover detection.
* **Kijun-sen (Base Line):** Slow moving average forming the crossover partner.
* **Senkou Span A / Senkou Span B:** Cloud boundaries evaluated on the previous bar to confirm bullish or bearish market structure.
* **Chikou Span (Lagging Line):** Momentum confirmation via lagging price breakout conditions.

The indicator lengths are user-configurable and match the defaults of the MQL5 expert (9 / 26 / 52).

### Entry Rules

A long position is opened when all of the following conditions are satisfied:

1. **Momentum trigger:** Either
   * Tenkan-sen crossed above Kijun-sen on the most recent closed bar (Tenkan<sub>t-1</sub> ≤ Kijun<sub>t-1</sub> and Tenkan<sub>t</sub> > Kijun<sub>t</sub>), or
   * The Chikou Span broke above historical price (Chikou<sub>t-1</sub> ≤ Close<sub>t-11</sub> and Chikou<sub>t</sub> > Close<sub>t-10</sub>),
2. **Cloud filter:** The current close is above both Senkou spans from the previous bar (price fully above the cloud),
3. **Price action filter:** The previous candle closed bullish (Close<sub>t-1</sub> > Open<sub>t-1</sub>),
4. **Position filter:** No long exposure is currently active. If a short position exists, it is closed first; the new long is submitted only after the short has been flattened.

A short position is opened under symmetric conditions:

1. **Momentum trigger:** Either
   * Tenkan-sen crossed below Kijun-sen (Tenkan<sub>t-1</sub> ≥ Kijun<sub>t-1</sub> and Tenkan<sub>t</sub> < Kijun<sub>t</sub>), or
   * The Chikou Span broke below historical price (Chikou<sub>t-1</sub> ≥ Open<sub>t-11</sub> and Chikou<sub>t</sub> < Open<sub>t-10</sub>),
2. **Cloud filter:** The current close is below both Senkou spans from the previous bar,
3. **Price action filter:** The previous candle closed bearish (Close<sub>t-1</sub> < Open<sub>t-1</sub>),
4. **Position filter:** Existing long exposure is closed before opening the short.

### Position Sizing and Martingale Option

* The base order size equals the strategy `Volume` property.
* When **Use Martingale** is enabled, the next entry size doubles if the previous completed trade closed with a loss. Profitable or breakeven trades reset the multiplier.
* The resulting order size is capped by `Volume × Max Position Multiplier`, mirroring the maximum-number-of-positions safeguard in the original EA.

### Risk Management

* **Static Stop-Loss / Take-Profit:** Optional absolute price offsets are applied to each new position. If the close price reaches the stop or target, the position is closed at market.
* **Trailing Stop:** When both `Trailing Stop Offset` and `Trailing Step` are positive, the stop level is tightened only after price advances beyond `offset + step` from the entry, emulating the incremental trailing logic from the MQL5 version.
* The strategy trades one net position. Upon exit (via stop, target, trailing, or reversal), the realized PnL is evaluated to update the martingale flag for the next signal.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| Tenkan Period | Length of the Tenkan-sen line. | 9 |
| Kijun Period | Length of the Kijun-sen line. | 26 |
| Senkou Span B Period | Length of the Senkou Span B line. | 52 |
| Stop Loss Offset | Absolute distance between entry price and protective stop. Set to 0 to disable. | 0 |
| Take Profit Offset | Absolute distance between entry price and profit target. Set to 0 to disable. | 0 |
| Trailing Stop Offset | Base trailing distance applied after activation. | 0 |
| Trailing Step | Additional movement required before tightening the trailing stop. | 0 |
| Max Position Multiplier | Upper bound for the effective order size (relative to `Volume`). | 5 |
| Use Martingale | Whether to double the next trade size after a losing trade. | true |
| Candle Type | Candle series used for calculations. | 1-hour time frame |

## Practical Notes

* The strategy requires at least 12 completed candles before all conditions can be evaluated (Chikou comparisons reference prices up to 11 bars back).
* Because StockSharp strategies operate on net positions, the `Max Position Multiplier` parameter caps the maximum contract size instead of managing multiple independent tickets. This keeps the behavior aligned with the exposure limit from the MQL5 implementation.
* Trailing-stop logic mirrors the EA: the stop is moved only when the price has progressed by `Trailing Stop Offset + Trailing Step`. Setting either parameter to zero disables trailing adjustments.
* Logging statements report every entry and exit, making it easy to audit decision points when replaying market data.

## Usage Workflow

1. Configure the desired candle type and instrument in a `StrategyContainer` or designer template.
2. Set base `Volume` and adjust risk parameters according to symbol volatility (e.g., convert pip-based distances from the original EA into price units for the selected market).
3. Start the strategy. Once the indicator has sufficient history, it will evaluate crossovers and lagging-line confirmations on each completed bar, automatically managing exits and martingale sizing.

