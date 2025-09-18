# Macd Pattern Trader v03 (StockSharp port)

## Overview
Macd Pattern Trader v03 is a high-level StockSharp strategy converted from the MetaTrader 4 expert advisor *MacdPatternTraderv03*. The original robot searches the MACD main line for a three-peak reversal formation and applies partial profit-taking rules based on moving averages. This C# port preserves the pattern logic while using StockSharp subscriptions, indicators, and order helpers.

The strategy is designed for trend exhaustion setups on liquid FX pairs, but it can be applied to any instrument that exposes a smooth MACD curve. The default timeframe is 30-minute candles, matching the original advisor, and the default trade size is one contract (or lot equivalent in StockSharp terms).

## Indicators and data flow
* **MACD (Fast EMA 5, Slow EMA 13, Signal 1)** — main indicator used to detect the triple-top/triple-bottom structure. The signal line is not used; the strategy relies on the MACD main line only.
* **EMA(7) and EMA(21)** — short and medium averages used during position management.
* **SMA(98) and EMA(365)** — slow filters that form the scaling-out trigger.

The implementation subscribes to the configured candle type and binds the indicators through `Bind` / `BindEx`. Only finished candles are processed to avoid acting on incomplete data.

## Entry rules
### Short setup
1. Arm the setup when the MACD main line rises above the **Upper Activation** level (default 0.0030).
2. Register the first peak once MACD prints a local maximum above both the previous and pre-previous values and then falls below the **Upper Threshold** (default 0.0045).
3. Register the second peak if MACD returns above the threshold, makes a higher local maximum, and drops back below the threshold again.
4. Confirm the pattern when a third rollover occurs with MACD staying below the threshold for three consecutive bars and the last local maximum is lower than the previous one.
5. If no long position exists, flatten any remaining long exposure and open a short position with the configured volume.

### Long setup
1. Arm the setup when the MACD main line drops below the **Lower Activation** level (default −0.0030).
2. Register the first trough once MACD prints a local minimum below the two previous values and then rises above the **Lower Threshold** (default −0.0045).
3. Register the second trough if MACD falls back under the threshold, reaches a lower minimum, and rises above the threshold again.
4. Confirm the bullish pattern when a third upswing is observed with MACD staying above the threshold for three candles and the latest trough is higher than the previous one.
5. Flatten any remaining short exposure and buy the configured volume.

The logic mirrors the nested `stops`, `stops1`, and `aop_ok*` flags in the original MQ4 file, including resets whenever MACD retraces past the activation band.

## Trade management
* **Scaling out** — when unrealized profit (calculated as `(Close − Entry) * Position`) exceeds `ProfitThreshold` (default 5 price units), the strategy applies two staged exits:
  * Stage 1 (long): previous candle close must stay above EMA(21). The strategy sells one-third of the initial long position. For shorts the requirement is the previous close below EMA(21) and one-third of the initial short volume is bought back.
  * Stage 2 (long): previous candle high must pierce the average of SMA(98) and EMA(365). Half of the original long position is closed. Shorts mirror this with the previous low dropping below the averaged filter.
* **Residual position** — whatever remains after the scaling sequence is left unmanaged by this port, matching the source EA.
* **Risk orders** — the MetaTrader version placed stop-loss and take-profit orders based on rolling highs and lows. Because StockSharp manages protective orders differently, this port does not auto-attach stops/targets. Users may combine the strategy with `StartProtection()` or an external risk module if required.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `Volume` | 1 | Trade size submitted on each entry. |
| `CandleType` | 30-minute time frame | Candle series used for indicator calculations. |
| `FastEmaLength` / `SlowEmaLength` | 5 / 13 | MACD fast and slow EMA periods. |
| `UpperThreshold` / `LowerThreshold` | 0.0045 / −0.0045 | Exhaustion band where pattern confirmations happen. |
| `UpperActivation` / `LowerActivation` | 0.0030 / −0.0030 | Outer band that arms the bearish/bullish setups. |
| `EmaOneLength` / `EmaTwoLength` | 7 / 21 | Auxiliary EMAs for visualization and scaling logic. |
| `SmaLength` | 98 | Slow SMA used together with EMA(365) during stage-two exits. |
| `EmaFourLength` | 365 | Long-term EMA used during stage-two exits. |
| `ProfitThreshold` | 5 | Minimum unrealized PnL (price * volume units) required before scaling out. |

## Practical notes
* Ensure the broker adapter supports partial position reduction. The original EA closed 1/3 and 1/2 portions; this port replicates the same fractions using market orders.
* Because protective orders are not attached automatically, consider enabling `StartProtection()` or adding custom risk rules if you need hard stops.
* The profit threshold is expressed in raw price * volume units. Adjust it according to the instrument's pip size or tick value to match the “5 currency units” assumption from the original MQ4 code.
* The strategy expects smooth MACD dynamics; excessive noise or illiquid instruments may prevent the three-peak logic from triggering.

## Differences from the MQ4 version
* Uses StockSharp indicator bindings instead of repeated `iMACD` calls.
* Unrealized profit calculation relies on `Position` and `PositionAvgPrice`, meaning broker rounding rules might differ from MetaTrader's `OrderProfit()`.
* Stop-loss and take-profit orders are not auto-generated; manual risk tools must be added if needed.
* The MQ4 parameter `sum_bars_bup` is not present because it was unused in the original source.
