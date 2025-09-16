# Dematus Strategy

## Overview
The Dematus Strategy replicates the logic of the original MetaTrader 5 "Dematus" expert advisor. It uses the DeMarker oscillator to detect momentum reversals and supports pyramiding with adaptive position sizing. The strategy is designed for a single instrument and trades on the candle series defined by the `CandleType` parameter.

Two DeMarker values are evaluated on every finished candle: the most recent value and the value from two bars ago. A crossover from the oversold threshold (0.3) to above signals long opportunities, while a crossover from the overbought threshold (0.7) to below signals short opportunities. After an initial entry, the strategy can add to the position if price travels by a configurable distance from the last executed entry price and the DeMarker signal fires again.

## Trading Rules
- **Primary entry:**
  - Open a long position when the DeMarker value from two bars ago is below 0.3 and the current value rises above 0.3, provided there is no open position.
  - Open a short position when the DeMarker value from two bars ago is above 0.7 and the current value falls below 0.7, provided there is no open position.
- **Scaling logic:**
  - While a position is active, the strategy remembers the exact price of the last fill. If price moves against the position by at least `DistancePips` (converted to price units) and the corresponding DeMarker crossover occurs again, the strategy submits an additional order in the same direction.
  - The size of each additional order is the previous executed volume multiplied by `VolumeMultiplier`, rounded to the instrument volume step and constrained by the exchange limits. This mirrors the lot coefficient behaviour of the original expert advisor.
- **Stop management:**
  - An initial stop loss is attached to each new position using `StopLossPips`. The stop level is recalculated after each scaling trade so the consolidated net position always has a valid protective level.
  - If `TrailingStopPips` is enabled, the stop level is tightened when the open profit exceeds `TrailingStopPips + TrailingStepPips`, emulating the trailing stop logic from the MQL implementation.
- **Equity protection:**
  - When flat, the strategy defines a virtual equity floor equal to `Balance - VirtualStopEquity`.
  - Once the floating equity rises by at least `TrailingStartEquity`, a trailing equity stop is activated and follows the peak equity minus `TrailingEquity`.
  - If account equity drops below the virtual floor while a position is open, all positions are liquidated immediately.

## Parameters
| Parameter | Description |
| --- | --- |
| `InitialVolume` | Base order size for the very first trade. Used again whenever the position is fully closed. |
| `DemarkerLength` | Period of the DeMarker indicator. |
| `StopLossPips` | Protective stop distance in pips applied to every entry. Set to zero to disable static stop loss. |
| `TrailingStopPips` | Trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | Additional favourable movement (in pips) required before the trailing stop is moved. Must be positive when trailing is active. |
| `DistancePips` | Minimum price distance (in pips) from the last fill before scaling into the position. |
| `TrailingEquity` | Distance between the equity peak and the protective equity floor. |
| `VirtualStopEquity` | Initial buffer below balance used to compute the virtual equity floor when the strategy is flat. |
| `TrailingStartEquity` | Profit threshold above balance that activates equity trailing. |
| `VolumeMultiplier` | Multiplier applied to the size of the last executed order when pyramiding. |
| `ResetEntryPrice` | When enabled, clears the stored entry price after every exit, preventing scaling until a new trade occurs. |
| `CandleType` | Candle data type (time frame) used for indicator calculations and signal generation. |

## Implementation Notes
- The strategy is implemented with the high-level StockSharp API. Candle subscriptions are handled through `SubscribeCandles`, and the DeMarker indicator is bound via `Bind` so indicator values arrive as ready-to-use decimals.
- Indicator state is tracked with simple scalar variables: the most recent value, the previous value, and the value from two bars back, exactly mirroring the buffer access pattern of the MQL source (`iDeMarkerGet(0)` and `iDeMarkerGet(2)`).
- Order volumes are rounded according to the security volume step and validated against minimum and maximum limits to prevent rejections.
- Equity control uses `Portfolio.CurrentValue` to mirror the balance/equity checks present in the original code. When the equity-based stop triggers, the strategy closes all open positions through market orders.
- The pip size is derived from `Security.PriceStep`. Instruments with three or five decimal places automatically receive the tenfold adjustment used in the MQL version to convert points to pips.

## Usage Notes
- Ensure the connected portfolio supplies current equity information so the equity trailing logic operates correctly.
- The strategy operates on finished candles only (`CandleStates.Finished`). It will ignore partially formed bars, matching the "new bar" gating logic of the original expert advisor.
- Default thresholds (0.3/0.7) are embedded in the code but can be adjusted by modifying the constants if required.
- The strategy supports live trading and backtesting. For backtests, verify that the portfolio simulator feeds equity values to allow the trailing equity logic to execute.
