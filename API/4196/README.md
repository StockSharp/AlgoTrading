# Trailing Stop FrCnSar Strategy

## Overview
The Trailing Stop FrCnSar strategy ports the MetaTrader toolkit shipped as **TrailingStopFrCnSARen_v4.mq4** and **OrderBalansEN_v3_4.mq4**. The expert advisor managed existing positions by adjusting their stop-losses using several techniques (previous candles, fractals, price velocity, or Parabolic SAR), while the companion indicator displayed the current account balance and open orders. The StockSharp conversion focuses on net positions and re-implements the trailing logic with high-level API primitives. It also provides an optional order summary logger so the informational overlay from the original indicator remains available in textual form.

The strategy does not open new trades automatically. Instead, it continuously observes the current position on `Strategy.Security`, updates the desired trailing stop level according to the selected mode and user-defined filters, and closes the exposure once price reaches the trailing barrier. Because StockSharp works with net positions rather than discrete tickets, all calculations apply to the aggregate quantity.

## Trading logic
1. Subscribe to the configured `CandleType` and process only finished candles to avoid premature stop adjustments.
2. Maintain short rolling buffers with candle highs and lows so that fractals and recent extremes can be retrieved without calling forbidden indicator methods.
3. Optionally calculate a smoothed close-to-close velocity in points when the velocity trailing mode is active.
4. For every completed candle, produce the candidate trailing stop price based on the selected mode:
   - Lowest low from the recent candle history minus the `DeltaPoints` offset.
   - Latest confirmed fractal adjusted by `DeltaPoints`.
   - Close price shifted by a velocity-dependent distance.
   - Current Parabolic SAR value offset by `DeltaPoints`.
   - A fixed distance expressed in instrument points.
5. Check the candidate against money-management filters: require existing stops, allow only profitable trailing, stop once break-even is achieved, or base the profit test on the average entry price.
6. Replace the stored stop level when the candidate improves the existing one by at least `StepPoints`.
7. If the candle crosses the stored level (low for longs, high for shorts) and trading is allowed, close the net position with a market order.
8. Optionally log a textual summary with balance, position size, entry price, current stop, and unrealised PnL, emulating the MetaTrader OrderBalans indicator.

## Trailing modes
- **Candle** – trails behind the most recent significant candle extreme. Offsets are applied via `DeltaPoints` to keep the stop slightly away from support/resistance.
- **Fractal** – uses the last five-bar fractal detected on the processed timeframe. This mimics the default MetaTrader implementation but operates on net positions.
- **Velocity** – estimates price velocity by averaging close-to-close changes over `VelocityPeriod`. When momentum accelerates in the position’s direction, the stop is tightened proportionally to the velocity difference scaled by `VelocityMultiplier`.
- **Parabolic** – follows the Parabolic SAR indicator managed by StockSharp. The stop hugs the SAR dots and inherits the step and maximum acceleration parameters.
- **Fixed points** – enforces a constant distance from price, effectively mirroring the “>4 pips” behaviour of the original script.
- **Off** – disables trailing and keeps the current stop untouched.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Mode` | `TrailingStopMode` | `Candle` | Determines which trailing algorithm is active. |
| `CandleType` | `DataType` | 15-minute candles | Timeframe used to analyse candles and calculate trailing data. |
| `DeltaPoints` | `int` | `0` | Additional distance (in instrument points) added below/above the raw trailing price. |
| `StepPoints` | `int` | `0` | Minimum improvement, in points, required before updating an existing trailing stop. |
| `FixedDistancePoints` | `int` | `50` | Distance for the fixed trailing mode. Ignored by other modes. |
| `TrailOnlyProfit` | `bool` | `true` | When `true`, trailing starts only after the stop would end in profit relative to the entry price. |
| `TrailOnlyBreakEven` | `bool` | `false` | Stop updating once the stored stop has moved beyond break-even. |
| `RequireExistingStop` | `bool` | `false` | Ignore trailing updates until a stop level has already been calculated. |
| `UseGeneralBreakEven` | `bool` | `false` | Evaluate the profitability filter using the average entry price of the net position (equivalent to the original `TProfit` helper). |
| `VelocityPeriod` | `int` | `30` | Number of closes used to average velocity in the velocity mode. |
| `VelocityMultiplier` | `decimal` | `1` | Scales the velocity adjustment applied to the trailing distance. |
| `ParabolicStep` | `decimal` | `0.02` | Acceleration step for the Parabolic SAR indicator. |
| `ParabolicMaximum` | `decimal` | `0.2` | Maximum acceleration for the Parabolic SAR indicator. |
| `LogOrderSummary` | `bool` | `true` | Enables textual logging similar to the OrderBalans panel. |
| `TradeVolume` | `decimal` | `1` | Default volume used when flattening positions via helper methods. |

## Differences from the original MetaTrader scripts
- The conversion works with StockSharp net positions instead of individual tickets. Stop updates therefore apply to the entire position regardless of how it was built.
- Magic number and multi-symbol filters were removed. The strategy monitors only `Strategy.Security` and assumes that position sizing is handled externally.
- The MetaTrader `Velocity` custom indicator is approximated through an averaged close-to-close difference measured in instrument points. This keeps the behaviour intuitive but may not match the proprietary indicator exactly.
- Visual chart objects (trendlines, arrows, labels) were replaced by textual log entries. The `LogOrderSummary` parameter re-creates the informational panel produced by *OrderBalansEN_v3_4.mq4* without relying on manual chart drawing.
- Stop modifications use StockSharp helper methods (`BuyMarket`, `SellMarket`) because the platform does not expose a direct equivalent to MetaTrader’s `OrderModify` on individual tickets.

## Usage tips
- Attach the strategy to a chart to visualise the effect of each trailing mode. For Parabolic SAR, enable the chart area to view dots and trades simultaneously.
- Adjust `DeltaPoints` and `StepPoints` according to the instrument tick size. The implementation automatically converts points using `Security.PriceStep` or `Security.MinPriceStep`.
- Keep `TrailOnlyProfit` enabled when mimicking the original behaviour, as the MetaTrader script avoided tightening stops before positions became profitable.
- Disable `LogOrderSummary` if you prefer a quieter output or are running hundreds of strategies concurrently.
- Test the velocity mode with different `VelocityMultiplier` values; higher multipliers make the trailing stop react faster to sudden bursts in momentum.

## Indicators
- Parabolic SAR (`ParabolicSar`)
- Rolling candle highs and lows (native data buffers)
- Optional averaged close-to-close velocity derived from candle closes
