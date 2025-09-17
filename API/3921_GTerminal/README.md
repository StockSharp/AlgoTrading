# GTerminal Strategy

## Overview
The GTerminal strategy is a C# port of the MetaTrader 4 expert advisor `GTerminal_V5a`. The original script allowed manual
control of entries and exits by drawing horizontal lines on the chart. This port recreates the same line-driven behaviour inside
the StockSharp framework by exposing each virtual line as a configurable parameter. Whenever the closing price of the selected
candle series crosses one of these virtual lines, the strategy opens, closes, or reverses positions in the same way as the MQL4
version. Optional automatic protection levels emulate the "tpinit" and "slinit" helper lines from the original tool.

## Strategy logic
### Price sampling
* The strategy works on finished candles of a user-defined timeframe (`CandleType`).
* `StartShift` controls which candle is used as the reference close. A value of `0` uses the current candle close, `1` uses the
previous candle, etc. The shift also affects the comparison candle so the script always evaluates two consecutive closes like the
MetaTrader implementation.
* `CrossMethod` mirrors the MQL4 input:
  * `0` – strict crossing: the previous close must be below (for long triggers) or above (for short triggers) the level and the
current close must finish on the opposite side of the level.
  * `1` – instant trigger: the current close only needs to be above/below the level. The port still checks the previous close to
prevent multiple triggers on the same bar, replicating the “touch once” behaviour obtained in MetaTrader by deleting the line
after it fires.

### Entry rules
* **Buy Stop line** – when the close moves from below to above `BuyStopLevel`, the strategy buys. If a short position is open,
the order size includes the volume required to flatten the short plus the configured `Volume` for the new long exposure.
* **Buy Limit line** – when the close falls through `BuyLimitLevel`, a long position is opened using the same volume logic.
* **Sell Stop line** – when the close moves from above to below `SellStopLevel`, the strategy sells. Existing longs are closed as
part of the order quantity.
* **Sell Limit line** – when the close rises through `SellLimitLevel`, a short position is opened.
* Entries are ignored when `Volume` is `0` or `PauseTrading` is enabled.

### Exit rules
* **Directional exits** – `LongStopLevel` and `LongTakeProfitLevel` close the long side when the close crosses the respective
line. `ShortStopLevel` and `ShortTakeProfitLevel` do the same for short exposure.
* **Global exits** – `AllLongStopLevel` / `AllLongTakeProfitLevel` liquidate every long position regardless of how it was opened.
`AllShortStopLevel` / `AllShortTakeProfitLevel` mirror the logic for shorts.
* **Initial protection** – setting `UseInitialProtection` to `true` applies the `InitialLongStopLevel`, `InitialLongTakeProfitLevel`,
`InitialShortStopLevel`, and `InitialShortTakeProfitLevel` immediately after a new position is filled. These levels behave like the
"slinit" / "tpinit" helper lines from the original script and remain active until the position is closed or the level is updated.
* Only one exit action is submitted per candle. When an exit condition is met, the strategy sends the closing order and skips the
remaining checks for that bar, just as the MQL4 version stopped after the line fired.

### Pause control
* `PauseTrading` reproduces the functionality of the MetaTrader "PAUSE" line. When enabled, no entry or exit logic is evaluated.
The state can be toggled manually without reloading the strategy.

## Parameters
* **Volume** – order volume for new entries. The final order size automatically includes any opposite exposure that must be
closed during a reversal.
* **Cross Method** – select the crossing algorithm (`0` strict, `1` instant).
* **Start Shift** – candle offset used for the crossing calculation.
* **Pause Trading** – disables all trading actions while `true`.
* **Use Initial Protection** – enables automatic application of the initial stop/take-profit levels after each fill.
* **Buy Stop Level / Buy Limit Level** – price levels that trigger long entries.
* **Sell Stop Level / Sell Limit Level** – price levels that trigger short entries.
* **Long Stop Level / Long Take Profit** – exit lines for the active long position.
* **Short Stop Level / Short Take Profit** – exit lines for the active short position.
* **All Long Stop / All Long Take Profit** – global exit lines that close every long position.
* **All Short Stop / All Short Take Profit** – global exit lines that close every short position.
* **Initial Long Stop / Initial Long Take Profit** – protective levels activated after each long entry when initial protection is
enabled.
* **Initial Short Stop / Initial Short Take Profit** – protective levels activated after each short entry when initial protection is
enabled.
* **Candle Type** – timeframe that supplies the closing prices used for comparisons.

## Implementation notes
* The port keeps the line-based workflow but exposes each line as a parameter instead of relying on chart objects. Users can
update levels on the fly through the parameter grid, mimicking the way lines were moved on a MetaTrader chart.
* Indicator window triggers from the original script (RSI, CCI, Momentum, etc.) are not available in this version. All triggers
use closing prices only. The parameter set can still be combined with other StockSharp components if indicator-driven behaviour
is required.
* The strategy relies solely on market orders (`BuyMarket`, `SellMarket`) just like the MQL4 script, which used market orders to
emulate pending line execution.
* There is no Python implementation; only the C# version is provided in this package.
