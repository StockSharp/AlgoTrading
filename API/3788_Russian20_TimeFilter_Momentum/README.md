# Russian20 Time Filter Momentum Strategy

## Overview
The **Russian20 Time Filter Momentum Strategy** is a conversion of the MetaTrader 4 expert advisor `Russian20-hp1.mq4`, originally distributed by Gordago Software Corp. The algorithm combines a 20-period simple moving average (SMA) with a 5-period Momentum indicator evaluated on 30-minute candles. Positions are only opened when price momentum and trend direction align, optionally restricted to a user-defined intraday trading window.

## Trading Logic
- **Data frequency:** Uses the configurable candle type (default: 30-minute candles, matching `PERIOD_M30` from the MT4 script). All signals are evaluated only on fully closed candles to stay faithful to the bar-close execution of the original expert.
- **Indicators:**
  - Simple Moving Average with adjustable length (default 20).
  - Momentum indicator with configurable lookback (default 5) and a neutral level set to 100, just like in MetaTrader.
- **Long entry:** Triggered when the following conditions align on the latest closed bar:
  1. The close price is above the SMA.
  2. Momentum prints above the neutral threshold (default 100).
  3. The current close price is higher than the previous candle close.
- **Short entry:** Triggered when:
  1. The close price is below the SMA.
  2. Momentum is below the neutral threshold.
  3. The current close price is lower than the previous close.
- **Exit rules:**
  - Long positions are closed when Momentum drops back to or below the threshold or when the take-profit target (if enabled) is hit.
  - Short positions are closed when Momentum rises to or above the threshold or when the take-profit target is achieved.

## Session Filter
The MetaTrader script offered an optional trading window (default 14:00–16:00). The StockSharp port exposes the same behaviour through the `UseTimeFilter`, `StartHour`, and `EndHour` parameters. When the filter is active, the strategy skips both entries and exits outside the selected hours, mirroring the original expert’s early return logic.

## Risk Management
The MQL4 version attached a fixed 20-pip take profit to every order. The conversion keeps this feature and expresses the distance in “pips,” automatically adjusting for fractional pip pricing (3/5 decimals) via the instrument’s `PriceStep`. Setting `TakeProfitPips` to zero disables the profit target entirely.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 30-minute candles | Data type used for price/indicator calculations. |
| `MovingAverageLength` | 20 | Lookback for the SMA trend filter. |
| `MomentumPeriod` | 5 | Lookback for the Momentum indicator. |
| `MomentumThreshold` | 100 | Neutral Momentum level used for entries and exits. |
| `TakeProfitPips` | 20 | Profit target distance in pips. Zero disables the target. |
| `UseTimeFilter` | false | Enables the intraday trading session filter. |
| `StartHour` | 14 | Inclusive start hour of the trading window (0–23). |
| `EndHour` | 16 | Inclusive end hour of the trading window (0–23). |

All parameters are defined through `StrategyParam<T>`, keeping them visible in the UI and ready for optimisation.

## Implementation Notes
- Uses the high-level `SubscribeCandles().Bind(...)` API so indicator values are streamed directly into the processing routine without manual series management.
- Stores only the latest close price to compare consecutive candles, avoiding heavy historical queries and complying with repository performance guidelines.
- Automatically recalculates the pip multiplier from `Security.PriceStep`, ensuring correct take-profit distances across Forex symbols with 4/5-digit pricing.
- Adds optional chart rendering hooks (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) for convenient visual analysis when the host environment supports it.

## Usage Tips
- Align the candle type with the timeframe you intend to trade; for Forex pairs the original 30-minute setting is a reasonable starting point.
- When `UseTimeFilter` is enabled, make sure `StartHour` is less than or equal to `EndHour`. Setting the start hour later than the end hour effectively disables trading because the MT4 logic simply skipped processing outside the specified interval.
- Because the expert never used a stop-loss, consider pairing the strategy with additional risk controls (manual or via StockSharp protective features) when trading live capital.
