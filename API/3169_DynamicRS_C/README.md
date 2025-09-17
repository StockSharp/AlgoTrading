# DynamicRS_C Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader expert advisor **Exp_DynamicRS_C** using the StockSharp high-level API. It evaluates the color transitions of the custom DynamicRS_C indicator to detect dynamic support and resistance. When the line turns magenta (color index `0`) it favours bullish setups, and when it turns blue-violet (color index `2`) it favours bearish setups. The StockSharp port keeps the same signal timing, permission flags, and stop/take structure as the source robot.

## Details

- **Entry Criteria**:
  - **Long**: The finished candle selected by `SignalBar` changes the indicator color from anything except `0` to `0`. The strategy optionally closes an existing short before entering, replicating the original `SellPosClose` gate, and then opens a long if `AllowBuyEntry` is enabled.
  - **Short**: The evaluated candle changes the indicator color from anything except `2` to `2`. The strategy optionally closes an existing long (`AllowBuyExit`) and then opens a short if `AllowSellEntry` is enabled.
- **Long/Short**: Trades both directions with independent toggles for entries and exits.
- **Exit Criteria**:
  - Long positions close when a short signal appears and `AllowBuyExit` is true, or when stop-loss / take-profit limits are hit.
  - Short positions close when a long signal appears and `AllowSellExit` is true, or when the risk limits trigger.
- **Stops**: `StopLossPoints` and `TakeProfitPoints` are absolute price offsets from the entry price. Set either value to zero to disable that protection.
- **Filters**:
  - `SignalBar` determines how many completed candles back are inspected for a color change, mimicking the original buffer lookup (`CopyBuffer(..., SignalBar, 2)`).
  - `CandleType` selects the timeframe used for both the indicator and trading logic (default: 4-hour candles, matching the EA).

## Parameters

- `CandleType` – Candle series processed by the strategy.
- `Length` – Lookback depth used by the DynamicRS_C indicator to compare highs/lows (`Length` in MQL).
- `SignalBar` – Number of fully closed candles back used for signal evaluation (equivalent to the EA input `SignalBar`).
- `AllowBuyEntry` / `AllowSellEntry` – Permit opening long/short positions on their respective signals.
- `AllowBuyExit` / `AllowSellExit` – Permit closing existing long/short positions when the opposite signal appears.
- `StopLossPoints` – Absolute loss distance from the entry price. When positive it closes longs below and shorts above the entry.
- `TakeProfitPoints` – Absolute profit distance from the entry price. When positive it closes longs above and shorts below the entry.
- `Volume` – Base order size inherited from `Strategy.Volume`. Additional quantity is automatically added to flatten opposite positions when the signal requests a reversal.

## Indicator Logic

The bundled `DynamicRsCIndicator` reproduces the colour-buffer behaviour of the MetaTrader script:

- It tracks the latest highs and lows over the configured `Length` window and the immediately preceding bar.
- When a local high is lower than both the previous high and the high `Length` bars ago, and also lies below the previous indicator value, the buffer switches to color `0` (magenta) and the value snaps to that high.
- When a local low is higher than both the previous low and the low `Length` bars ago, and above the previous indicator value, the buffer switches to color `2` (blue-violet) and the value snaps to that low.
- Otherwise the indicator keeps its previous value. Neutral color `1` acts as a bridge between trend states exactly as in the original algorithm.

By binding this indicator through `BindEx`, the strategy receives both the numeric value and the discrete color index, ensuring that signal evaluation and trade timing match the behaviour of the source expert.
