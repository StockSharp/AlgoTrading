# SilverTrend Duplex Strategy

## Overview

The **SilverTrend Duplex Strategy** is a StockSharp port of the MetaTrader 5 expert advisor `Exp_SilverTrend_Duplex`. The original robot combines two independent SilverTrend filters (for long and short decisions) and executes trades when the indicator colours flip between bullish and bearish states. This C# implementation keeps the dual-filter architecture, allowing you to tune the long and short logic separately while taking advantage of StockSharp's high level API.

The strategy operates on finished candles only. Two separate subscriptions can be configured, so long and short signals may observe different timeframes or instruments if required. Internally, a custom `SilverTrendIndicator` rebuilds the colour logic from the MQL version by combining Donchian channel extremes with the risk multiplier to emulate the original SilverTrend bands.

## Trading Logic

1. **Indicator reconstruction**
   - For each candle the Donchian upper and lower bounds over `SSP` bars are calculated.
   - Adaptive thresholds `smin` and `smax` are derived using the risk coefficient (`33 - risk`), identical to the MQL algorithm.
   - When price closes above `smax` a bullish state is recorded, when it closes below `smin` a bearish state is recorded, otherwise the previous state is retained. Candle body direction determines the final colour code (0..4) exactly as in the original SilverTrend indicator.

2. **Signal preparation**
   - Colour values are stored for the most recent `SignalBar + 1` finished candles for both long and short filters.
   - Long signals trigger when the colour at the selected offset drops below `2` (bullish) while the previous colour was greater than `1` (non-bullish), replicating `Value[1] < 2 && Value[0] > 1` from MQL.
   - Short signals trigger when the colour rises above `2` (bearish) and the previous colour is above `0`, matching `Value[1] > 2 && Value[0] > 0` from the script.

3. **Order execution**
   - Entries use `BuyMarket` or `SellMarket` with a volume equal to `Volume + |Position|`, which both closes any opposite exposure and opens the new side in a single market order.
   - Exits rely on the indicator reverting to the opposite colour band. Long positions are closed when the colour moves above `2`, short positions when it drops below `2`.

The strategy does not recreate the original money-management matrix or server-side stop placement from `TradeAlgorithms.mqh`. Risk control should therefore be managed via StockSharp's built-in protective mechanisms or broker rules.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `LongCandleType` | 4 hour candles | Data type used for the long-side indicator. |
| `LongSsp` | 9 | SilverTrend lookback length for the long filter. |
| `LongRisk` | 3 | Risk multiplier (`33 - risk`) applied to the channel width. |
| `LongSignalBar` | 1 | Offset (in finished candles) for evaluating long signals. Must be ≥ 1. |
| `EnableLongEntries` | true | Toggles opening of long positions. |
| `EnableLongExits` | true | Toggles closing of long positions when bearish colours appear. |
| `ShortCandleType` | 4 hour candles | Data type used for the short-side indicator. |
| `ShortSsp` | 9 | SilverTrend lookback length for the short filter. |
| `ShortRisk` | 3 | Risk multiplier for the short filter. |
| `ShortSignalBar` | 1 | Offset for evaluating short signals. Must be ≥ 1. |
| `EnableShortEntries` | true | Toggles opening of short positions. |
| `EnableShortExits` | true | Toggles closing of short positions when bullish colours appear. |
| `Volume` | 1 | Base order volume used for entries. |

## Implementation Notes

- Signals are evaluated only after both the indicator and the colour history contain enough data (`SignalBar + 1` values). This mirrors the `BarsCalculated` checks from the MQL expert.
- The custom indicator exposes decimal colour values instead of copying raw buffer data. No direct calls to `GetValue` are required thanks to the `Bind` high-level API.
- When long and short candle types are identical, two subscriptions are created intentionally to keep the parameter sets isolated. This matches the dual-handle behaviour in the original advisor.
- Stop-loss, take-profit, deviation and margin management options from the source script are not replicated. You can add StockSharp risk rules (e.g. `StopLossRule`) if similar behaviour is needed.

## Usage Tips

- Optimise `LongSsp`, `ShortSsp` and corresponding risk values separately to adapt the breakout thresholds to each market regime.
- If you want to emulate the original "signal on previous bar" behaviour, keep `SignalBar` at `1`. Larger values force the strategy to wait additional bars before reacting.
- Combine the strategy with portfolio-level risk controls or time filters when running on multiple instruments, as the SilverTrend colour flip can produce frequent regime changes on choppy markets.
