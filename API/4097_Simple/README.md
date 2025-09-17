# Simple Strategy

## Overview
The **Simple Strategy** is the StockSharp high-level conversion of the MetaTrader 4 expert advisor `S!mple.mq4` located in `MQL/9019`. The original system monitored a fixed basket of Forex symbols and traded whenever a 50-period linear weighted moving average crossed a 200-period simple moving average. Each entry could be repeated a configurable number of times and optional money-based stop-loss and take-profit levels were attached to every trade. The conversion keeps the same logic, exposes all user inputs as strategy parameters, and logs the same diagnostic information that the EA printed in the MetaTrader terminal comment.

## Trading Logic
1. **Data preparation.** The strategy subscribes to a configurable candle type (five-minute candles by default) and binds both moving averages through the high-level `SubscribeCandles().Bind(...)` API.
2. **Moving average crossover.** Two historical values of every moving average are buffered. A buy signal occurs when the fast LWMA was below the slow SMA two bars ago and closed above it on the previous finished bar. A sell signal is detected when the inverse condition happens.
3. **Trend margin tracking.** The slow SMA value that occurred `TrendMargin` bars ago is cached to reproduce the EA's textual trend report. The live slow SMA is compared against that reference to classify the background trend as `UP`, `DOWN`, or `WAIT`, together with the distance expressed in price steps.
4. **Execution model.**
   - When a buy signal is triggered, any short exposure is closed before buying up to `NumOrders * TradeVolume`. The requested volume mirrors the EA behaviour where several identical orders were stacked until the maximum count was reached.
   - A sell signal closes long exposure first and then sells up to the same aggregated target volume.
5. **Protective levels.** Optional money-based stops and targets (`StopLossMoney`, `TakeProfitMoney`) are translated into price distances using the instrument `PriceStep`/`StepPrice` and the per-order `TradeVolume`. Once the levels are stored, every finished candle checks the high/low range; if a level is breached the position is flattened at market.
6. **Operational guard.** Actual order placement is executed only when `EnableTrading` is set to `true`, replicating the original `makeTrades` flag that let the EA run in an "analysis only" mode.

## Risk Management and Money Stops
- Stop-loss and take-profit amounts are interpreted as cash risk/target per entry block (per MetaTrader order). The conversion uses the security metadata (`PriceStep`, `StepPrice`) to convert that amount into a rounded number of price steps. If either field is missing, a warning is logged and the monetary stops remain disabled.
- Protective levels are evaluated on the high/low of each completed candle, matching the tick-level checks done by the EA while staying within StockSharp's high-level framework.
- `StartProtection()` is invoked on start so that account-level protections configured in StockSharp remain active while the strategy runs.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume of a single MetaTrader-like order. The base `Strategy.Volume` is kept in sync with this value. |
| `NumOrders` | `1` | Maximum number of volume blocks that can be accumulated in the same direction. The final target volume equals `TradeVolume * NumOrders`. |
| `StopLossMoney` | `0` | Optional stop-loss amount in account currency per volume block. Set to zero to disable the stop. |
| `TakeProfitMoney` | `0` | Optional take-profit amount in account currency per volume block. Set to zero to disable the target. |
| `TrendMargin` | `10` | Number of finished candles used to produce the background trend text (slow SMA compared with its value `TrendMargin` bars ago). |
| `FastLength` | `50` | Length of the fast linear weighted moving average. |
| `SlowLength` | `200` | Length of the slow simple moving average. |
| `EnableTrading` | `false` | When `false` the strategy only logs signals, exactly like the EA when `makeTrades=false`. |
| `CandleType` | `5m time-frame` | Candle type used for indicator calculations. |

## Notes on the Conversion
- The MetaTrader EA iterated through six hard-coded Forex symbols. StockSharp strategies operate on the `Strategy.Security` supplied by the user. To reproduce the basket trading behaviour, either launch several instances of the strategy (one per instrument) or wrap them inside a parent strategy that dispatches the same signals to multiple securities.
- Money-based protective levels rely on the instrument metadata. For Forex pairs make sure both `PriceStep` and `StepPrice` are filled (for example, `0.0001` and the pip value per one lot). Otherwise the stop/target distance is silently treated as zero after logging a warning.
- The log message emitted on every finished candle mirrors the EA comment: it lists the signal (`BUY`, `SELL`, or `WAIT`), both moving averages, the distance between them in price steps, and the trend assessment obtained from the delayed slow SMA.
- The number of stacked orders is modelled as an aggregate target volume. This keeps the total exposure identical to the original implementation while using StockSharp's high-level market order helpers instead of multiple individual `OrderSend` calls.
- No Python port is created yet, matching the task requirements.

## Usage Tips
- Assign a Forex security with correctly configured `PriceStep`, `StepPrice`, and `VolumeStep` values. Set `TradeVolume` to your desired lot size and enable trading once you are satisfied with the logged diagnostics.
- To mimic the EA default behaviour (analysis only), leave `EnableTrading` at `false`. When you are ready to trade, flip it to `true` and the next crossover signal will submit market orders.
- Because protective levels are monitored on candle closes, consider using shorter candles if you need tighter intrabar reaction compared with the tick-by-tick behaviour of MetaTrader.
