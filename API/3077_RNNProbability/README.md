# RNN Probability Strategy

## Overview
The RNN Probability strategy is a conversion of the MetaTrader expert *RNN (barabashkakvn's edition)*. The original robot collects three RSI snapshots separated by the RSI period and feeds them into a hand-crafted probability lattice that emulates a recurrent neural network. The StockSharp port replicates this behaviour with the high-level candle subscription API, automatically converting the MetaTrader lots, price steps, and stop/target distances into StockSharp concepts.

Once the RSI value of the latest finished candle becomes available, the strategy looks back by one and two RSI periods to build a three-point history. These normalized readings are combined with the eight MetaTrader weights (`Weight0` … `Weight7`) to produce a probability that the market should fall. The probability is remapped into the `[-1; 1]` range, and the sign determines whether to open a long or short position. Only one position at a time is maintained, matching the original Expert Advisor.

## Trading logic
1. Subscribe to the configured candle series and process the `RelativeStrengthIndex` indicator manually using the selected `AppliedPrice` input (open by default).
2. Store the finished RSI values in a rolling buffer large enough to access the RSI reading from one and two full periods back.
3. Normalise the three RSI values to the `[0; 1]` range and evaluate the neural network lattice:
   - The first branch (`Weight0`, `Weight1`, `Weight2`, `Weight3`) handles the case when the current RSI is in the lower half (below 50).
   - The second branch (`Weight4`, `Weight5`, `Weight6`, `Weight7`) handles the case when the current RSI is in the upper half.
4. Transform the resulting probability into a trade signal between `-1` and `+1`.
5. If no position is open and the signal is negative, buy `TradeVolume` lots. If the signal is non-negative, sell `TradeVolume` lots instead.
6. Optionally arm symmetric stop-loss and take-profit levels expressed in pips. The strategy automatically converts the pip distance to an absolute price offset, including the extra digit adjustment used by MetaTrader for 3- and 5-digit forex symbols.
7. Log each decision with the RSI inputs, probability, and resulting signal, mirroring the chatty behaviour of the source expert.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-hour time frame | Primary candle series used for indicator updates and signal generation. |
| `TradeVolume` | `decimal` | `1` | Lot size sent with each market order. |
| `RsiPeriod` | `int` | `9` | Length of the RSI indicator. Also defines the distance between the historical RSI samples. |
| `AppliedPrice` | `AppliedPriceType` | `Open` | Price component forwarded to the RSI (Open, Close, High, Low, Median, Typical, Weighted). |
| `StopLossTakeProfitPips` | `decimal` | `100` | Pip distance for both stop-loss and take-profit. Set to zero to disable protective orders. |
| `Weight0` … `Weight7` | `decimal` | `6, 96, 90, 35, 64, 83, 66, 50` | Probability weights applied to the eight lattice branches. Each value represents a percentage between 0 and 100. |

## Differences from the original MetaTrader expert
- Email notifications were removed. StockSharp logs provide the same insight without relying on an SMTP server.
- Position sizing is fixed to a single `TradeVolume`. Partial closures or incremental scaling are intentionally omitted to match the one-position design of the source code.
- Indicator data is delivered through StockSharp's high-level candle subscription, eliminating manual `CopyBuffer` calls and pointer arithmetic.
- Pip conversion uses the instrument's `PriceStep` and automatically compensates for 3/5-digit forex symbols instead of relying on hard-coded tick sizes.

## Usage tips
- Align `TradeVolume` with the instrument's minimum lot step before launching the strategy; the constructor also mirrors the value into `Strategy.Volume`.
- Tune the eight weights during optimisation to adapt the neural network lattice to different markets. All weights are exposed as optimisation parameters.
- Decrease `StopLossTakeProfitPips` or set it to zero when running on symbols with wide spreads or when using discretionary exits.
- Add the strategy to a chart to visualise candles, RSI, and executed trades for easier validation of the neural-network output.

## Indicators
- One `RelativeStrengthIndex` calculated from the chosen applied price.
