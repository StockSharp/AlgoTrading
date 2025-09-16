# Color Ma RSI Trigger Duplex Strategy

This strategy ports the **Exp_ColorMaRsi-Trigger_Duplex.mq5** expert advisor to the StockSharp high level API.
It drives two independent MaRsi-Trigger detectors: the **long block** decides when long positions should be
opened or closed, while the **short block** performs the same task for short positions. Each detector evaluates
whether a custom indicator reports bullish (`+1`), neutral (`0`) or bearish (`-1`) market pressure. The original
MetaTrader logic is preserved, including the delayed confirmation that waits for two completed bars before reacting
and the separate money-management settings per direction.

## Trading idea

1. Compute two exponential moving averages (fast and slow) and two RSI oscillators (fast and slow) on a selectable
   candle series for each block.
2. At every finished candle the indicator returns `+1` when both fast studies dominate their slow counterparts,
   `-1` when both are weaker and `0` otherwise. The raw value is clamped to the range `[-1, 1]` as in the MT5 indicator.
3. The strategy stores a rolling history of indicator values. For a configured `SignalBar` offset it compares the value
   of the bar `SignalBar + 1` periods ago (named `older`) with the value of the bar `SignalBar` periods ago (named `recent`).
4. Long logic:
   - If `older < 0` the long block closes any active long position (provided long exits are enabled).
   - If `older > 0` **and** `recent <= 0` the long block prepares a new long entry (provided long entries are enabled).
5. Short logic mirrors the long block:
   - If `older > 0` the short block exits existing short positions (when short exits are enabled).
   - If `older < 0` **and** `recent >= 0` the block opens a new short position (when short entries are enabled).
6. Optional stop-loss and take-profit levels, expressed in instrument price steps, flatten positions when price
   crosses the configured levels.

The two blocks can subscribe to different candle time-frames and price sources, allowing the user to replicate the
original dual-time-frame behaviour or experiment with alternative combinations.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `LongCandleType`, `ShortCandleType` | Candle data series used by the long and short blocks. Defaults to 4-hour candles. |
| `LongVolume`, `ShortVolume` | Market volume traded when a new position is opened by the corresponding block. |
| `LongAllowOpen`, `ShortAllowOpen` | Enable or disable opening of new positions for each block. |
| `LongAllowClose`, `ShortAllowClose` | Enable or disable closing signals for each block. |
| `LongStopLossPoints`, `ShortStopLossPoints` | Stop-loss distance measured in price steps. Set to `0` to disable. |
| `LongTakeProfitPoints`, `ShortTakeProfitPoints` | Take-profit distance measured in price steps. Set to `0` to disable. |
| `LongSignalBar`, `ShortSignalBar` | Number of completed bars between the current candle and the one used for the decision logic. |
| `LongRsiPeriod`, `LongRsiLongPeriod`, `ShortRsiPeriod`, `ShortRsiLongPeriod` | Lengths of the fast and slow RSI oscillators. |
| `LongMaPeriod`, `LongMaLongPeriod`, `ShortMaPeriod`, `ShortMaLongPeriod` | Lengths of the fast and slow moving averages. |
| `LongRsiPrice`, `ShortRsiPrice` | Price source fed into the fast RSI (close, open, high, low, median, typical or weighted). |
| `LongRsiLongPrice`, `ShortRsiLongPrice` | Price source fed into the slow RSI. |
| `LongMaPrice`, `ShortMaPrice` | Price source fed into the fast moving average. |
| `LongMaLongPrice`, `ShortMaLongPrice` | Price source fed into the slow moving average. |
| `LongMaType`, `ShortMaType` | Moving-average method for the fast line (simple, exponential, smoothed or weighted). |
| `LongMaLongType`, `ShortMaLongType` | Moving-average method for the slow line. |

## Trading rules

1. Wait until the selected candle series produces finished bars and all indicators are fully warmed up.
2. For each block compute the MaRsi-Trigger value and update the history buffer.
3. When the history contains at least `SignalBar + 2` entries evaluate the long and short conditions described in the
   trading idea section.
4. Before opening a position the strategy will neutralise any opposite exposure (if the corresponding close flag is enabled).
   For example, a new long entry will buy enough volume to close a short position and only then add the long volume.
5. After a position is opened, optional stop-loss and take-profit levels are monitored on every finished candle.
6. Opening and closing orders are sent as market orders through the high-level `BuyMarket` and `SellMarket` helpers.

## Risk management

* Stops and targets are measured using `Security.PriceStep`. When the instrument does not expose a price step, a default
  value of `1` is assumed, matching the behaviour of many existing strategies in this repository.
* Long and short blocks maintain independent stop and take settings.
* The strategy does not place additional protective orders (such as trailing stops); the behaviour mirrors the MT5 expert,
  which closes trades only when the indicator fires or when the hard stop/target is hit.

## Notes

* The StockSharp port issues market orders immediately after the evaluating candle is finished. In MetaTrader the expert
  scheduled its orders for the opening time of the next bar via timestamp offsets; both behaviours effectively align because
  StockSharp processes the signal as soon as the candle closes.
* The original EA exposed several money-management modes (`LOT`, `BALANCE`, etc.). StockSharp strategies work with direct
  volume values, therefore the port keeps the volume as a straightforward parameter (`LongVolume`/`ShortVolume`).
* Slippage and magic-number specific logic from the MT5 helper library is not necessary in StockSharp and has been omitted.
* Indicator calculations leverage the built-in StockSharp moving averages and RSI implementations; the output is clamped to
  `[-1, 1]` to match the original `ColorMaRsi-Trigger` indicator.
