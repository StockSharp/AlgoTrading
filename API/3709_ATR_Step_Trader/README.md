# ATR Step Trader Strategy

## Overview
The ATR Step Trader strategy is a direct port of the MetaTrader5 expert advisor `atrTrader.mq5`. It combines a fast/slow moving-average filter with Average True Range (ATR) based breakout and pyramiding rules. The port keeps the bar-driven workflow of the original EA: only completed candles are processed, the fast SMA must sit above or below the slow SMA for a fixed number of bars, and every decision is anchored to ATR multiples to normalise distances across markets.

## Indicators and data
- **Simple Moving Averages (SMA).** Two moving averages (`FastPeriod` and `SlowPeriod`) define the primary trend filter. Both are applied to the subscription candle series.
- **Average True Range (ATR).** A `AverageTrueRange` indicator (`AtrPeriod`) converts volatility into price distances. Every breakout, add-on, and stop calculation uses ATR multiples.
- **Highest/Lowest price channels.** `Highest` and `Lowest` indicators track the extreme high and low of the most recent `MomentumPeriod` candles. They replace the `iHighest`/`iLowest` calls from the MQL code.
- **Time frame.** The default candle type is one hour (`TimeSpan.FromHours(1)`), mirroring the `PERIOD_CURRENT` behaviour of the original script. You can switch to any other timeframe by editing the `CandleType` parameter.

## Entry logic
1. Wait for the candle to finish. Unfinished candles are ignored to stay in sync with the MT5 OnTick + iTime guard.
2. Update the moving-average streak counters. A bullish streak increments when the fast SMA prints above the slow SMA; a bearish streak increments when it prints below. Mixed readings reset the opposite streak.
3. Once the bullish streak reaches `MomentumPeriod`, check whether the closing price is still below the recent high by at least `StepMultiplier * ATR`. If so, buy at market.
4. Once the bearish streak reaches `MomentumPeriod`, check whether the closing price is still above the recent low by at least `StepMultiplier * ATR`. If so, sell at market.
5. Every new position initialises directional state: the strategy remembers the highest and lowest filled prices per side so that later pyramids have reference anchors. The first order also attaches a volatility-sized stop (`StepMultiplier * StopMultiplier * ATR`).

## Position management
- **Pyramiding:** While the number of active entries is below `PyramidLimit`, the strategy adds another unit whenever price moves either `+/- StepsMultiplier * ATR` away from the current extreme reference. This mirrors the “Steps” scaling grid from the EA and works in both favourable and unfavourable directions.
- **Protective stops:** The initial stop for a new order sits `StepMultiplier * StopMultiplier * ATR` away from the fill price. When the pyramid is full, stops are tightened to `StepMultiplier * ATR` behind (for longs) or ahead (for shorts) of the latest close, emulating the EA’s trailing update when three positions are open.
- **Adverse exits:** If price retreats by `StepsMultiplier * ATR` beyond the tracked extreme, the strategy immediately exits all positions on that side with a market order. This captures the EA logic that dumps the full stack when price breaks the most recent ladder edge.
- **State reset:** After a full exit, the streak counters and ATR stop references reset so that a new trend sequence must develop before re-entry.

## Parameters
| Group | Name | Description | Default |
| --- | --- | --- | --- |
| Trend Filter | `FastPeriod` | Fast SMA length that measures short-term direction. | `70` |
| Trend Filter | `SlowPeriod` | Slow SMA length that measures long-term direction. | `180` |
| Trend Filter | `MomentumPeriod` | Number of consecutive finished candles that must confirm the trend. | `50` |
| Volatility | `AtrPeriod` | ATR window used for all distance calculations. | `100` |
| Entry Logic | `StepMultiplier` | ATR multiple that gates initial breakouts. | `4` |
| Entry Logic | `StepsMultiplier` | ATR multiple that separates pyramid layers. | `2` |
| Risk Management | `StopMultiplier` | Extra multiplier applied to the initial stop beyond the base step distance. | `3` |
| Position Sizing | `PyramidLimit` | Maximum number of entries per direction. | `3` |
| Trading | `TradeVolume` | Strategy volume submitted with each market order. | `1` |
| General | `CandleType` | Candle type (time frame) used for the subscription. | `TimeFrame(1h)` |

## Practical notes
- The StockSharp version uses the strategy `Volume` property for sizing. Adjust `TradeVolume` to match your instrument contract size before going live.
- Market orders are assumed to fill immediately, just like MT5’s use of `CTrade.Buy`/`Sell`. In thin markets you may want to replace the market orders with limit or stop orders.
- The high/low references replicate the EA’s `h_price` and `l_price` variables and are updated whenever a new layer is added or removed. They are essential for determining when to add or flush the ladder.
- Because the EA stored stop losses per position while StockSharp manages them at the strategy level, the port applies the tightest stop logic to the entire stack. This delivers the same behaviour (all positions exit together) with fewer broker-side orders to manage.
- Always test the strategy in simulation. ATR distances adapt to volatility, but in markets with gaps or high slippage the realised risk can still exceed the projected stop distance.
