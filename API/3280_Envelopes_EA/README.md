# Envelopes EA Strategy

## Overview
The strategy replicates the MetaTrader 4 expert advisor "EnvelopesEA". It applies an exponential moving average envelope to the primary candle stream and trades mean reversions. When the market pushes far outside the envelope a contrarian market order is sent. Positions are closed as soon as price re-enters the envelope in the opposite direction. The original expert was tested on EUR/USD in 2019; the StockSharp port keeps the same logic while exposing all key inputs as optimizable parameters.

## Trading Logic
1. Calculate an exponential moving average (EMA) of `EnvelopePeriod` length on the selected candles.
2. Build an upper and a lower envelope by expanding the EMA with `UpperDeviationPercent` and `LowerDeviationPercent` respectively.
3. Apply an additional entry buffer defined by `EntryOffsetPoints` (multiplied by the instrument price step) to avoid premature trades.
4. When there is no open position:
   - Enter long if the close price falls below the lower envelope minus the entry buffer.
   - Enter short if the close price rises above the upper envelope plus the entry buffer.
5. When a position exists:
   - Close long positions once the close price crosses back above the upper envelope.
   - Close short positions once the close price crosses back below the lower envelope.

The strategy always holds at most one open position and uses market orders for both entries and exits.

## Money Management
Order volume is specified directly through the `Volume` parameter (lots). There are no automatic martingale or pyramiding rules, keeping the behaviour identical to the latest MQ4 implementation where scaling features were disabled by default.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Volume` | Order volume in lots. | 0.2 |
| `EnvelopePeriod` | Length of the EMA forming the envelope basis. | 50 |
| `UpperDeviationPercent` | Percentage deviation applied to the upper band. | 0.5 |
| `LowerDeviationPercent` | Percentage deviation applied to the lower band. | 0.5 |
| `EntryOffsetPoints` | Extra distance, in price steps, that price must travel beyond the band before entering. | 100 |
| `CandleType` | Timeframe used for candles and indicator calculations. | 30-minute candles |

All numeric parameters (except `CandleType`) are marked as optimizable to help reproduce the original optimisation workflows.

## Notes
- The envelope uses an EMA instead of the SMA from earlier versions because the MQ4 script evolved towards an exponential basis in the latest iteration. This offers faster reaction to price swings and improves mean-reversion timing.
- The entry buffer is multiplied by the instrument `PriceStep`. Ensure that the security metadata contains a valid step size; otherwise the strategy falls back to a conservative `0.0001` default.
- Chart visualisation includes price candles, the EMA envelope and the strategy trades, making it easy to validate signal behaviour against the original Expert Advisor.
