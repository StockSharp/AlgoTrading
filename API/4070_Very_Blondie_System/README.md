# Very Blondie System

## Overview
The Very Blondie System is a short-term mean-reversion grid strategy originally distributed as the MetaTrader 4 expert advisor "VBS - Very Blondie System". The port keeps the original idea of fading a breakout from the recent trading range: when price moves far enough away from the highest high or lowest low seen over the last `PeriodX` candles, the strategy enters immediately with a market order and adds four martingale-style limit orders to scale into the move if price keeps extending.

## Data and Indicators
- **Primary data**: a single candle series configured by the `CandleType` parameter (MQL version trades on the chart timeframe).
- **Indicators**: `Highest` and `Lowest` indicators (length = `PeriodLength`) track the rolling range extremes used for breakout detection.
- **Level 1 quotes**: best bid/ask prices are consumed to place market and limit orders at the original MT4 offsets.

## Entry Logic
1. On each finished candle compute the highest high and lowest low over the last `PeriodLength` bars.
2. Read the current best bid/ask (fallback to the candle close if quotes are missing).
3. **Long setup**: if `highest - bid > LimitPoints * PointValue`, submit a buy market order with the base volume and place four buy limit orders below the ask. Each limit order sits `GridPoints * PointValue` further away and doubles the volume of the previous order (1×, 2×, 4×, 8×, 16×).
4. **Short setup**: if `bid - lowest > LimitPoints * PointValue`, submit a sell market order and four sell limit orders above the bid at the same distances and volume multipliers as the buy logic.
5. Only one basket can be active at a time. New signals are ignored until every position and pending order from the previous cycle is gone.

## Position Management
- **Floating profit target**: the original `Amount` parameter monitored `OrderProfit + OrderSwap` across all trades. The port reproduces this with the aggregated position: `(close - entryPrice) * position * conversionFactor >= ProfitTarget`. When the threshold is reached, every position is closed with market orders and all remaining grid orders are cancelled.
- **Lockdown break-even**: when `LockDownPoints > 0`, the MT4 code moved the stop-loss of each filled order to `entry price ± Point` once the trade was `LockDownPoints` points in profit. The StockSharp version tracks the net position; as soon as price advances by `LockDownPoints * PointValue` the break-even level is armed at `entryPrice ± PointValue`. If a later candle touches that level (low for longs, high for shorts), the entire basket is flattened and all pending orders are cancelled.
- **Manual exits**: stopping the strategy or hitting the profit/break-even conditions always cancels the four pending limit orders to mimic the `CloseAll()` routine from MT4.

## Money Management
- **Base volume**: matches the MT4 expression `MathRound(AccountBalance()/100) / 1000`. The strategy reads the current portfolio value (or beginning value when no trades were made), rounds it away from zero and converts it to lots. The result is aligned to `Security.VolumeStep`, obeys `MinVolume`/`MaxVolume`, and falls back to the strategy `Volume` (or `1`) when the portfolio snapshot is unavailable.
- **Martingale grid**: each additional limit order doubles the base volume up to four levels (1×, 2×, 4×, 8×, 16×). Volumes are normalized with the same helper to avoid sending fractional lots that the venue rejects.
- **PointValue parameter**: MT4's `Point` may differ from `Security.PriceStep` (especially on 5-digit FX quotes). `PointValue` defaults to automatic detection from `PriceStep`/`Step`, but you can override it to match the original EA's behaviour precisely.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `PeriodLength` | Lookback window for the highest high and lowest low | `60` |
| `LimitPoints` | Minimum distance (in MT4 points) between current price and the range extreme to trigger a basket | `1000` |
| `GridPoints` | Spacing (in MT4 points) between consecutive grid orders | `1500` |
| `ProfitTarget` | Floating profit target expressed in account currency | `40` |
| `LockDownPoints` | Profit distance (in MT4 points) that arms the break-even exit | `0` |
| `PointValue` | Price change produced by one MT4 point (`0` = auto-detect) | `0` |
| `CandleType` | Candle series used to drive the strategy | `TimeFrameCandle, 1 minute` |

## Porting Notes
- Floating PnL is approximated with the aggregate position rather than summing each order's `OrderProfit + OrderSwap`. This matches the original behaviour when all trades are in the same direction, which is how the EA operates.
- Stop-loss modification is emulated by an immediate market exit at the armed break-even price; StockSharp keeps the logic in the strategy layer instead of sending `OrderModify` requests.
- Pending limit orders are registered with normalized prices using `Security.ShrinkPrice`. When the security metadata lacks a `PriceStep`, set `PointValue` manually to avoid misaligned grids.
- The strategy assumes one instrument and uses high-level API helpers (`SubscribeCandles`, `SubscribeLevel1`, `BuyLimit`, `SellLimit`, etc.) as requested in the conversion guidelines.
