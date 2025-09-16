# Triple SMA Spread Strategy

## Overview
This strategy is a C# port of the MetaTrader 5 expert advisor `3sma.mq5` (id 21495). It follows the same idea of trading when three simple moving averages separate from each other by a configurable spread. The implementation uses the high-level StockSharp API with candle subscriptions and indicator binding so that no manual series management is required.

## Original MT5 behaviour
The MT5 expert relies on three simple moving averages with different periods and display shifts. The fast average uses the current bar, while the middle and slow averages are shifted one and two bars into the past. On every tick it:

1. Converts the user-defined spread from pips into price units based on symbol precision.
2. Closes long positions when the fast SMA drops below the middle SMA by at least half of the spread, and closes short positions when the fast SMA rises above the middle SMA by half of the spread.
3. Opens new long positions if `MA1 > MA2 + spread` and `MA2 > MA3 + spread` while no other long trades from the expert remain. Analogously, it opens short positions when all three averages are aligned in the opposite order.
4. Uses only market orders with a fixed lot size and does not apply explicit stop-loss or take-profit levels.

## StockSharp implementation
* Indicators – three `SimpleMovingAverage` instances subscribe to the same candle source. Compact history buffers reproduce the MT5 "shift" parameters so that each comparison uses finished-bar values from the requested offsets.
* Spread handling – the spread parameter is entered in pips. The strategy derives a pip size from `Security.PriceStep` (or `Security.Step`) and multiplies it by ten for 3/5-digit FX symbols, matching the MT5 adjustment for fractional quotes.
* Order flow – orders are submitted with `BuyMarket`/`SellMarket`. When a reversal condition appears, the strategy adds the absolute value of the current net position to the base volume in order to flatten the opposite exposure and establish the new direction with a single market order.
* Visualization – if charts are available, the strategy plots the source candles together with the three moving averages and executed trades.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `Volume` | Order volume used for each market entry. | `0.1` |
| `FastMaPeriod` | Period of the fast SMA (equivalent to MA1 in MT5). | `9` |
| `FastMaShift` | Number of finished bars used to shift the fast SMA. | `0` |
| `MiddleMaPeriod` | Period of the middle SMA (MA2). | `14` |
| `MiddleMaShift` | Shift in finished bars for the middle SMA. | `1` |
| `SlowMaPeriod` | Period of the slow SMA (MA3). | `29` |
| `SlowMaShift` | Shift in finished bars for the slow SMA. | `2` |
| `MaSpreadPips` | Minimal required spread between consecutive SMAs measured in pips. | `10` |
| `CandleType` | Candle series used for calculations. | `1 minute` time-frame |

## Trading logic
1. Wait for all three moving averages to be formed and for the history buffers to contain values for the requested shifts.
2. Convert the spread parameter from pips into price units and compute half-spread for exit filters.
3. **Exit filters** –
   * Close long exposure if the shifted fast SMA falls below the shifted middle SMA by at least half of the spread.
   * Close short exposure if the shifted fast SMA rises above the shifted middle SMA by at least half of the spread.
4. **Entry conditions** –
   * Enter long (or reverse from short to long) when the fast SMA is greater than the middle SMA plus the spread **and** the middle SMA is greater than the slow SMA plus the spread.
   * Enter short (or reverse from long to short) when the fast SMA is less than the middle SMA minus the spread **and** the middle SMA is less than the slow SMA minus the spread.

## Differences from the MT5 version
* StockSharp works with a single net position per security. When a reversal signal appears the strategy issues a single market order sized to both flatten the previous net exposure and establish the new one. The MT5 expert could keep independent long and short positions.
* Pip conversion uses the best available `Security` metadata. If the broker supplies neither `PriceStep` nor `Step`, a value of `1` is used as a fallback.
* Orders are submitted on finished candles instead of every tick because the high-level API operates on candle subscriptions.
* The strategy does not implement the verbose logging helpers from the MT5 code; StockSharp built-in logging can be used if needed.

## Usage notes
* Ensure that the selected candle series matches the timeframe used in the original MT5 setup.
* Adjust the spread parameter whenever the instrument uses non-standard pip sizes.
* Because the strategy works with finished candles, execution will be delayed until the current candle closes.
