# Lot Fraction Adjuster Strategy

## Overview
This sample reproduces the `afd.AdjustLotSize` helper from the MetaTrader script in folder `MQL/8582`. The original code rounded a raw lot size so it fits the broker's `MODE_LOTSTEP` and an additional fraction constraint before printing the intermediate values with a series of `Alert` calls. The StockSharp port exposes the same behaviour inside a strategy that can be attached to any security. As soon as the strategy starts it evaluates the instrument's `VolumeStep`, performs the rounding procedure, stores every intermediate value, and reports the results through the logging subsystem.

Unlike a full trading robot, this sample focuses exclusively on money-management normalisation. It demonstrates how to read volume metadata from `Security`, how to replicate MetaTrader's rounding rules, and how to publish diagnostic information with `AddInfoLog` and `AddWarningLog`.

## Lot adjustment logic
1. Read the instrument's minimal lot increment from `Security.VolumeStep`. If the value is missing or non-positive the adjustment cannot be performed; the strategy issues a warning and stops processing.
2. Convert the requested lot size (`InputLotSize`) to step units by dividing it by the lot step.
3. Round the step count to the nearest integer using `MidpointRounding.AwayFromZero` so the behaviour matches MetaTrader's `MathRound` function.
4. Reduce the rounded step count to the closest multiple of `Fractions` via `Math.Floor`. This reproduces the original `MathFloor(MathRound(...) / Fractions) * Fractions` sequence.
5. Multiply the adjusted step count by the lot step to obtain the final tradable size. The strategy stores the final size together with every intermediate figure and prints them in a single `AddInfoLog` entry. When the adjusted lot is positive it is also assigned to `Strategy.Volume` so helper order methods use the same size.

The calculated values are exposed through read-only properties (`LotStep`, `StepsInput`, `StepsRounded`, `StepsOutput`, `AdjustedLotSize`) to simplify automated checks during further development.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `InputLotSize` | `decimal` | `0.58` | Raw order size requested by the user before any normalisation. |
| `Fractions` | `int` | `2` | Number of minimal lot steps that must form a single tradable block. Values below `1` are internally clamped to `1`. |

## Runtime behaviour
- `OnStarted` performs the entire adjustment sequence and writes a descriptive log entry containing the input size, lot step, intermediate counters, and the resulting lot size.
- If the security lacks a valid `VolumeStep`, a warning is reported and none of the cached values are updated beyond their default `0` states.
- Whenever the adjusted size is greater than zero the strategy also copies it into the `Volume` property so calls such as `BuyMarket()` immediately use the normalised quantity.

## Differences versus the MetaTrader helper
- MetaTrader displayed every value with multiple `Alert` calls; the StockSharp version aggregates them into a single `AddInfoLog` message and uses `AddWarningLog` for error conditions.
- The conversion relies on `Security.VolumeStep` instead of `MarketInfo(Symbol(), MODE_LOTSTEP)`. This allows the strategy to work with any provider supported by StockSharp as long as volume metadata is available.
- Additional read-only properties expose the intermediate results to make unit testing or UI binding easier, something the original script did not provide.

## Usage tips
- Ensure the selected security exposes a `VolumeStep` (for example by downloading the instrument definition from the broker) before starting the strategy.
- Adjust `InputLotSize` and `Fractions` to match the broker's contract specifications. A fraction of `2` means only even numbers of minimal steps can be traded.
- Attach the strategy to a chart or enable logging to observe the computed values and verify that the rounding sequence matches your expectations.

## Logged values
The informational log entry prints the following sequence, matching the order of the MetaTrader `Alert` statements:
- `AdjustedLotSize`
- `StepsOutput`
- `StepsRounded`
- `StepsInput`
- `LotStep`
- `Fractions`
- `InputLotSize`
