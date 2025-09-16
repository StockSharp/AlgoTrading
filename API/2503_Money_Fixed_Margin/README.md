# Money Fixed Margin Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader "Money Fixed Margin" example using StockSharp's high-level API. It showcases how to size positions by risking a fixed percentage of the portfolio while converting stop-loss distance expressed in pips to an absolute price offset. The strategy only trades long positions and focuses on demonstrating the money management logic rather than a predictive entry signal.

## Details

- **Entry Criteria**:
  - **Long**: executes a market buy after every completed candle count specified by `Check Interval` (defaults to every 980th bar). The order uses the closing price of the triggering candle as the reference for risk calculations.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Protective stop-loss is automatically attached via `StartProtection` at a distance derived from the `Stop Loss (pips)` parameter.
  - No profit target is used; positions close only by the stop-loss or manual intervention.
- **Stops**: Stop Loss only.
- **Default Values**:
  - `Stop Loss (pips)` = 25
  - `Risk Percent` = 10
  - `Check Interval` = 980
  - `Candle Type` = 1-minute time frame
- **Filters**:
  - Category: Risk Management
  - Direction: Long
  - Indicators: None
  - Stops: Yes (stop-loss)
  - Complexity: Basic
  - Timeframe: Intraday (configurable through `Candle Type`)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium (scales with `Risk Percent`)

## Position Sizing Logic

1. The strategy reads `Security.PriceStep` and `Security.Decimals` to infer the pip size. Pairs with 3 or 5 decimal places use a tenfold multiplier to match MetaTrader's definition of a pip.
2. `Stop Loss (pips)` is multiplied by the pip size to obtain an absolute price distance (`ExtStopLoss`) identical to the MQL5 code.
3. The current portfolio value (preferring `Portfolio.CurrentValue` then `Portfolio.BeginValue`) is multiplied by `Risk Percent / 100` to determine the capital exposed per trade.
4. Risk per single lot is computed through the product of the stop-loss distance, the number of price steps within that distance, and `Security.StepPrice` when available. If `StepPrice` is unknown, the price distance itself is used as a fallback.
5. Dividing the risk amount by the risk per lot yields the desired volume. The result is normalized to the security's `VolumeStep`, clamped to minimum and maximum volume limits, and logged for transparency. A comparison value with zero stop-loss distance is also logged to illustrate why the money manager refuses trades without a protective stop.

## Workflow

1. On start the strategy subscribes to the configured candle series, calculates the pip size, and enables `StartProtection` with the computed absolute stop-loss distance.
2. Each finished candle increments an internal counter. When the counter reaches the chosen `Check Interval`, the strategy evaluates position size, prints diagnostic information, and resets the counter.
3. If the computed volume is positive, a market buy order is placed. The built-in protection attaches the stop-loss at `Close - ExtStopLoss`. Any errors (e.g., due to insufficient data or zero-priced instruments) prevent order submission.
4. No further trades are taken until the counter completes another interval, keeping the focus on money management rather than signal frequency.

## Usage Notes

- Set `Risk Percent` to a conservative value when connecting to a live account; the default 10% risk mirrors the MQL example but is aggressive for real trading.
- Ensure that the security provides meaningful `PriceStep` and `StepPrice` metadata. When unavailable, the strategy still operates but interprets risk in raw price units.
- The strategy intentionally avoids short trades to stay faithful to the original demonstration. Adapt `BuyMarket`/`SellMarket` calls if two-sided trading is desired.
- Combine this money management module with other signal generators by reusing the `CalculateFixedMarginVolume` helper from the strategy code.
