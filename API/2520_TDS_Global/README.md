# TDS Global Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the original MetaTrader "TDSGlobal" expert based on Alexander Elder's Triple Screen concept. It evaluates
daily candles and combines the slope of MACD (12, 23, 9) with a 24-period Williams %R filter. The system seeks to buy when the
trend is turning up while %R shows oversold conditions, and to sell when the trend turns down and %R signals overbought.

Whenever a valid setup is detected, the strategy places stop orders beyond the previous session's high or low. Entries are
pushed away from the current market by a configurable buffer to avoid entering too close to the price, mirroring the original
"16 point" offset logic. Once in a position the strategy manages a protective stop, optional take profit and a trailing stop in
price steps.

## Trading Logic

- **Data**: Works on daily candles by default (configurable).
- **Trend filter**: Compares two most recent MACD main-line values. Rising MACD implies long bias, falling MACD implies short bias.
- **Oscillator filter**: Uses the previous Williams %R value. Below `WilliamsBuyLevel` (default -75) allows long setups, above
  `WilliamsSellLevel` (default -25) allows short setups.
- **Entry**:
  - Long: place a buy-stop above the prior high plus one price step. The entry is lifted to at least `EntryBufferSteps` price
    steps above the last close to keep a minimum distance from the market.
  - Short: place a sell-stop below the prior low minus one price step. The order is lowered to at most the last close minus
    `EntryBufferSteps` steps.
- **Risk management**:
  - Initial stop is anchored to the opposite extreme of the previous candle (high for shorts, low for longs).
  - Take profit distance equals `TakeProfitSteps` price steps. The default value (999) keeps the behaviour close to the MQL
    version that used a very wide target.
  - Trailing stop is enabled when `TrailingStopSteps` > 0. It follows the close by that many steps and only tightens in the
    direction of the trade.
- **Order handling**:
  - Existing stop orders are cancelled and refreshed whenever the entry price or protective levels need to be updated.
  - Opposite trend signals remove pending orders that no longer align with the MACD direction.
  - When a position opens the stored pending levels are reused to initialise the live stop/take prices.
- **Optional staggering**: The original EA staggered order placement across FX pairs to avoid simultaneous pending orders.
  Setting `UseSymbolStagger` to `true` enforces the same minute windows for EURUSD, GBPUSD, USDCHF and USDJPY.

## Parameters

- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACD periods used for the trend slope check.
- `WilliamsLength` – lookback for Williams %R.
- `WilliamsBuyLevel`, `WilliamsSellLevel` – oversold/overbought thresholds (negative values, closer to -100/-0 respectively).
- `EntryBufferSteps` – minimum offset from the current market when placing stop entries (number of price steps).
- `TakeProfitSteps` – target distance in price steps (set a small number to activate a hard target).
- `TrailingStopSteps` – trailing stop distance in steps; set to zero to disable trailing.
- `UseSymbolStagger` – enables the symbol-specific minute windows.
- `CandleType` – timeframe for candles (daily by default).

## Notes

- Use the strategy volume to control the lot size; it defaults to 1 if no volume is specified.
- Pending orders and trailing exits operate on completed candles, so fills between candle closes are approximated by the
  stored entry price.
- The take profit default is large to match the original EA behaviour; adjust it when you need a finite target.
