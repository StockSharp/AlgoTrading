# Exp MA Rounding Candle MMRec Strategy

## Overview
The **Exp MA Rounding Candle MMRec Strategy** is the StockSharp port of the MQL5 expert advisor `Exp_MA_Rounding_Candle_MMRec`. The original system relies on a custom "MA Rounding Candle" indicator that converts every market candle into a smoothed synthetic candle and tracks its color changes. The C# version reproduces the same behaviour by rebuilding the indicator logic on the fly and reacting to the resulting color stream.

## MA Rounding Candle construction
1. Every incoming candle is processed by four identical moving averages (open, high, low, close). The supported smoothing types are **Simple**, **Exponential**, **Smoothed (RMA/SMMA)**, and **Weighted** moving averages.
2. The raw moving-average output is passed through the original "rounding" filter. The filter only accepts a new value if it differs from the previous output by more than `RoundingFactor * PriceStep`. Otherwise the previous rounded value is kept. This reproduces the MQL5 behaviour where the signal stays flat during small oscillations.
3. A gap filter anchors the rounded open to the previous rounded close whenever the absolute difference between the real open and close is smaller than `GapSize * PriceStep`. This prevents small doji candles from changing the synthetic candle color.
4. After rounding, the indicator color is defined as:
   * `2` – bullish synthetic candle (`open < close`)
   * `0` – bearish synthetic candle (`open > close`)
   * `1` – neutral candle (`open == close`)

The strategy stores only the last few color values (enough for the configured look-back) and does not keep any long history, in line with the original expert.

## Signal logic
Signals are evaluated on finished candles using a configurable `SignalBar` offset:

* Let `SignalBar` denote how many closed candles back should be treated as the trigger bar (`0` = current closed bar, `1` = the most recent fully closed bar, etc.).
* The strategy also inspects the color of the bar that immediately precedes it (`SignalBar + 1`).
* A **bullish-to-non-bullish** transition (`color[SignalBar + 1] = 2` and `color[SignalBar] != 2`) generates:
  * optional closing of existing short positions (`EnableShortExits`), and
  * optional opening of a new long position (`EnableLongEntries`).
* A **bearish-to-non-bearish** transition (`color[SignalBar + 1] = 0` and `color[SignalBar] != 0`) generates:
  * optional closing of existing long positions (`EnableLongExits`), and
  * optional opening of a new short position (`EnableShortEntries`).

Position management follows the original EA: exits are executed before new entries, and when switching direction the strategy adds the absolute value of the existing position to the base trading volume so that the net size matches the desired direction.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1-hour time frame | Candle series used to drive the strategy. |
| `SmoothingMethod` | `Simple` | Moving-average type for all rounded price series. |
| `MaLength` | `12` | Number of periods used by the chosen moving average. |
| `RoundingFactor` | `50` | Multiplier applied to the instrument `PriceStep` to build the rounding threshold. Larger values make the rounded series change less frequently. |
| `GapSize` | `10` | Multiplier applied to the `PriceStep` for the gap filter that locks the rounded open to the previous rounded close on small candles. |
| `SignalBar` | `1` | How many closed candles back are analysed for the signal. |
| `TradeVolume` | `1` | Base position volume used for new entries. The parameter is synchronised with the built-in `Strategy.Volume` property. |
| `EnableLongEntries` / `EnableShortEntries` | `true` | Toggles for long/short entries. |
| `EnableLongExits` / `EnableShortExits` | `true` | Toggles for closing existing positions. |

## Implementation notes
* Only the smoothing modes available in StockSharp are exposed. Exotic MQL5-specific smoothers (JJMA, JurX, VIDYA, AMA, etc.) are not present in this port.
* The complex money-management recounter from the original EA is replaced with a single `TradeVolume` parameter. This keeps the strategy deterministic and easier to optimise inside StockSharp.
* All price-based thresholds (`RoundingFactor`, `GapSize`) are interpreted in price steps by multiplying the value by `Security.PriceStep` each time a candle is processed.
* The strategy uses the high-level candle subscription API (`SubscribeCandles`) and operates strictly on completed candles, just like the MQL5 expert that waits for `IsNewBar` before issuing orders.
* Long/short protection, trailing stops, and other exits are intentionally omitted because they were not part of the original implementation.

## Usage
1. Attach the strategy to the desired security and assign an appropriate candle series through `CandleType` (e.g., `TimeSpan.FromHours(1).TimeFrame()`).
2. Configure the smoothing method, moving-average length, rounding factor, and gap filter to match the original EA settings or your own optimisation results.
3. Set `TradeVolume` to the lot size you plan to trade. The strategy automatically synchronises the internal `Volume` property with this parameter.
4. Enable or disable long/short entries and exits depending on the desired behaviour.
5. Start the strategy. Trades will be generated whenever the MA Rounding Candle color performs the configured transitions.

The README reflects the C# implementation contained in `CS/ExpMaRoundingCandleMmrecStrategy.cs` and should be used as reference documentation for this port.
