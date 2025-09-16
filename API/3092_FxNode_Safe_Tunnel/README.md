# FxNode Safe Tunnel Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader 4 expert advisor *FxNode - Safe Tunnel*. The system uses a ZigZag-based trend channel: the most recent swing highs are connected to form a resistance line while swing lows create a support line. A position is opened when the market price touches one of the channel boundaries within a configurable tolerance and all safety checks pass.

The conversion follows the original workflow but adapts it to the high-level StockSharp API:

- Candle subscription drives the logic. Only fully formed candles are processed.
- A `Highest`/`Lowest` pair emulates the ZigZag detector used to draw the tunnel trendlines.
- An `AverageTrueRange` indicator provides the volatility-based stop anchor that the MQL version produced with `ATRCheck() * 10`.
- Level1 quotes are monitored so the strategy can enforce a maximum spread before allowing new trades.

## Entry logic

1. Detect swing highs and lows with a configurable ZigZag depth, deviation (in pips) and backstep. The newest two highs and two lows define the trendlines.
2. Compute the price of each trendline at the current candle close time and measure the vertical distance between the latest swing high and low.
3. Long setup: the best ask price must stay above the lower trendline but not farther than the `TouchDistanceBuyPips` buffer. Shorts mirror the condition around the upper trendline and the best bid.
4. Optional session filter (defaults to midnight–06:00) must allow trading. The strategy also blocks new orders on Friday, Saturday and Sunday, mimicking the original `AllowToOrder()` restrictions.
5. The current spread (ask – bid) must not exceed `MaxSpreadPips` when quotes are available.
6. `MaxOpenPositions` controls the maximum net exposure. Because StockSharp uses netting, this value acts as a cap on total position volume rather than on separate tickets.

## Exit logic

- Initial stop-loss: the original EA placed it at `ATR * 10`. The port keeps the same multiplier while respecting the `MaxStopLossPips` cap.
- Initial take-profit: defaults to the distance between the most recent swing high and low, but it is limited by `TakeProfitPips` when configured.
- Fixed profit target: if `FixedTakeProfitPips` is greater than zero the position is closed once the price gains at least that many pips from the entry.
- Trailing stop: once the candle close moves by more than `TrailingStopPips` in favour of the trade, the stop-loss is tightened to lock in profits.
- Weekend exit: when `CloseBeforeWeekend` is enabled, any open position is closed after 23:50 on Friday.

All exits are executed with market orders to stay consistent with the original behaviour.

## Risk and sizing

The lot size is calculated using three stages:

1. Try to risk `RiskPercentage` of the portfolio value, assuming both the instrument price step and monetary step value are known.
2. If risk sizing cannot be computed, fall back to `StaticVolume`.
3. Clamp the final volume between `MinVolume` and `MaxVolume`.

Because StockSharp reports a single net position per instrument, the original `MaxOpenPosition` limit is interpreted as a maximum total exposure rather than a count of independent tickets.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | 30 minute candles | Primary timeframe for analysis and trading. |
| `TrendPreference` | Both | Choose long-only, short-only or symmetric trading. |
| `TakeProfitPips` | 800 | Maximum take-profit distance in pips (0 disables the limit). |
| `MaxStopLossPips` | 200 | Maximum stop-loss distance in pips (0 disables the limit). |
| `FixedTakeProfitPips` | 0 | Early exit distance expressed in pips. |
| `TouchDistanceBuyPips` | 20 | Long entries require the ask price to stay within this buffer above the lower trendline. |
| `TouchDistanceSellPips` | 20 | Short entries mirror the buffer requirement near the upper trendline. |
| `TrailingStopPips` | 50 | Trail distance applied after the trade becomes profitable. |
| `StaticVolume` | 1 | Fallback order volume when risk-based sizing is not possible. |
| `MinVolume` / `MaxVolume` | 0.02 / 10 | Bounds for the final order volume. |
| `MaxSpreadPips` | 15 | Maximum allowed spread in pips for new entries. |
| `RiskPercentage` | 30 | Portfolio percentage risked per trade. Set to 0 to always use `StaticVolume`. |
| `MaxOpenPositions` | 1 | Maximum net exposure (in multiples of the current order volume). |
| `UseTimeFilter` | true | Enables the trading window. |
| `SessionStart` / `SessionEnd` | 00:00 / 06:00 | Trading window. When the start is later than the end the window wraps through midnight. |
| `CloseBeforeWeekend` | true | Close any position after 23:50 on Friday. |
| `AtrPeriod` | 14 | ATR lookback used for the stop calculation. |
| `ZigZagDepth` | 5 | ZigZag lookback depth. |
| `ZigZagDeviationPips` | 3 | Minimum distance between consecutive pivots (in pips). |
| `ZigZagBackstep` | 1 | Bars between eligible pivots. |
| `ZigZagHistory` | 10 | Number of stored pivots for trendline projection. |

## Notes and limitations

- The ZigZag reconstruction mirrors the MQL behaviour by combining the `Highest`/`Lowest` indicators with deviation and backstep filters. If the instrument trades on a custom session, consider adjusting the parameters to align with the original indicator.
- Spread filtering requires live best bid/ask quotes. When quotes are absent (for example during backtesting with candle-only data) the spread filter is skipped.
- The port operates with net positions. Environments that require independent ticket management should extend the strategy to track each fill separately.
- Time strings from the MQL version (e.g., `"24:00"`) are replaced with `TimeSpan` parameters. To reproduce an overnight session set the start later than the end, for example 23:30 to 05:30.

## Usage

1. Attach the strategy to an instrument, configure the candle type and parameters, and run it in simulation or live mode.
2. Ensure market depth or Level1 subscriptions are enabled to enforce the spread filter accurately.
3. Review and adjust the risk controls before trading on real capital.
