# Pinball Machine Strategy

## Overview
This strategy is a direct StockSharp conversion of the MetaTrader 4 expert advisor `Pinball_machine.mq4`. The original robot drew
random integers on every incoming tick and opened a market order whenever two of those draws matched. The StockSharp version
preserves the same lottery-style behaviour: on each finished candle of the selected timeframe the algorithm performs two pairs of
random draws and enters a long or short market position when the corresponding pair contains equal values. Stop-loss and take-profit
distances are also randomised on every evaluation, reproducing the feel of the original "pinball" routine where trades bounce in and
out unpredictably.

## Trading logic
- Subscribe to candles defined by the `CandleType` parameter and wait for fully formed bars.
- For every finished candle generate four integers uniformly distributed in `[0, RandomMaxValue]`. The first pair belongs to the
  potential long entry, the second pair belongs to the potential short entry.
- Draw two additional integers between `MinStopLossPoints`/`MaxStopLossPoints` and `MinTakeProfitPoints`/`MaxTakeProfitPoints` to
  determine the protective distances (expressed in price steps) shared by both sides of the evaluation.
- If the first and second random integers match, submit a market buy order with volume `TradeVolume`. If the third and fourth
  values match, submit a market sell order with the same volume. Both conditions can fire within the same candle, exactly like in
  the MQL version where buy and sell orders were independent events.
- Immediately attach a stop-loss and a take-profit order (if the drawn distance is greater than zero). The distances are interpreted
  as multiples of the instrument’s `PriceStep`, mirroring the `Point` multiplier used in MetaTrader.

## Order management and risk controls
- `StartProtection()` is invoked when the strategy starts so that StockSharp manages protective orders on behalf of the strategy.
- Each entry measures the resulting position (`Position ± TradeVolume`) and passes it to `SetStopLoss` and `SetTakeProfit`, which
  allows the platform to consolidate protective orders even when multiple trades are running at the same time.
- If either the minimum or maximum distance parameters are set to zero or a negative number, the corresponding protection is
  skipped for that cycle.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Order size in lots/contracts submitted for every random entry. |
| `CandleType` | Timeframe of the candles that trigger the random draws. Shorter periods emulate the original tick-based EA more closely. |
| `RandomMaxValue` | Inclusive upper bound for the integer draws. A larger value lowers the probability of matching numbers and therefore reduces trade frequency. |
| `MinStopLossPoints` | Lower bound (in price steps) for the randomly generated stop-loss distance. |
| `MaxStopLossPoints` | Upper bound (in price steps) for the stop-loss distance. |
| `MinTakeProfitPoints` | Lower bound (in price steps) for the randomly generated take-profit distance. |
| `MaxTakeProfitPoints` | Upper bound (in price steps) for the take-profit distance. |
| `RandomSeed` | Seed of the pseudo-random number generator. Zero keeps the behaviour time-based, any other value makes the sequence reproducible. |

## Implementation notes
- The MetaTrader script was tick-driven; the StockSharp port uses candle completions because the high-level API operates on time-series events. Setting a very short `CandleType` (for example one-second or tick candles) restores the fast-paced nature of the original.
- Stop-loss and take-profit values are generated once per evaluation and reused for both the long and short branches, exactly like in the source EA.
- Ensure that the traded instrument exposes a valid `PriceStep`, otherwise protective distances expressed in points may need manual adjustment.
