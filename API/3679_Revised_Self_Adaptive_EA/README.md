# Revised Self Adaptive EA

Port of the MetaTrader 5 expert advisor `revised_self_adaptive_ea.mq5` into the StockSharp high level strategy framework.

## Strategy overview

The algorithm scans a configurable candle series and looks for engulfing reversal setups confirmed by momentum and trend filters:

* **Pattern detection** – evaluates the last closed candle against the previous one. A bullish setup requires a green body that opens below the previous close while the previous candle is bearish. The mirror logic is applied for bearish setups. Candle bodies are compared against a rolling average to filter out weak signals.
* **Momentum filter** – a classic RSI ensures that bullish trades only trigger from oversold territory and bearish trades from overbought conditions.
* **Trend filter** – a short simple moving average must agree with the trade direction. This prevents fading strong trends without confirmation.
* **Risk management** – ATR driven stop-loss and take-profit levels are calculated for every new position. Optional trailing stops keep following profitable moves while never reducing protection. Positions are force-closed when price hits the protective levels.
* **Spread and risk guard** – trades are skipped whenever the current spread exceeds the configured threshold or when the ATR based stop would risk more than the allowed percentage of price.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Candle aggregation used for analysis. Defaults to one hour bars. |
| `AverageBodyPeriod` | Number of candles used to compute the average body size filter. |
| `MovingAveragePeriod` | Length of the simple moving average that acts as a directional filter. |
| `RsiPeriod` | RSI length used for oversold/overbought confirmation. |
| `OversoldLevel` | RSI threshold that must be met before accepting a bullish reversal. |
| `OverboughtLevel` | RSI threshold that must be met before accepting a bearish reversal. |
| `AtrPeriod` | ATR length used for volatility based protective distances. |
| `StopLossAtrMultiplier` | Multiplicative factor applied to ATR for the stop-loss distance. |
| `TakeProfitAtrMultiplier` | Multiplicative factor applied to ATR for the take-profit distance. |
| `TrailingStopAtrMultiplier` | ATR distance maintained by the trailing stop logic. |
| `UseTrailingStop` | Enables the trailing stop supervisor. |
| `MaxSpreadPoints` | Maximum allowed spread (expressed in price steps/pips). Signals are ignored when the market is wider. |
| `MaxRiskPercent` | Maximum acceptable percentage risk based on the ATR stop relative to the entry price. |
| `TradeVolume` | Base lot size used for market orders. |

## Behaviour notes

* Positions are flattened before reversing direction to mirror the MetaTrader implementation.
* Protective stop/take levels are recomputed after every fill using the most recent ATR reading.
* The trailing stop only moves in the trade direction and is disabled when ATR data is not yet available.
* If the strategy is running on an instrument without reliable bid/ask quotes the spread filter will stay inactive automatically.

## Differences vs. original MQL

The original script only outlined the signal detection routine. In this port the missing elements were reconstructed using the provided parameters:

* Added moving-average confirmation to make use of the MA handle declared in the MQL source.
* Implemented ATR based stop-loss, take-profit, and trailing stop logic using the volatility handle defined in the original expert.
* Added a risk-percentage guard so that oversized ATR stops are skipped instead of blindly executing.
* Visualization elements (chart arrows) were omitted because StockSharp strategies do not draw objects on charts by default.

## Usage

1. Attach the strategy to a portfolio and security inside Hydra or your custom StockSharp host.
2. Ensure the candle subscription matches the intended timeframe (default: one hour).
3. Adjust the risk parameters to reflect the instrument’s volatility.
4. Start the strategy. It will automatically subscribe to candles, compute indicators, and place market orders when conditions are satisfied.
