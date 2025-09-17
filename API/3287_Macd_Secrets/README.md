# Macd Secrets Strategy

## Overview
The **Macd Secrets Strategy** is a multi-timeframe momentum-following system inspired by the original "Macd Secrets I" expert advisor for MetaTrader. The StockSharp port uses the high-level API and focuses on aligning MACD direction across three timeframes while filtering trades with a linear weighted moving average (LWMA) baseline and a momentum deviation check. The strategy only holds a single net position at any moment, providing a simplified and transparent risk profile compared to the source EA that could pyramid multiple orders.

## Signal generation
### Long setup
1. Fast LWMA is below the slow LWMA on the execution timeframe, signalling that price is trading near the lower side of the trend channel (the original EA applies the same filter).
2. MACD line is above its signal line on all tracked timeframes: execution, trend confirmation, and monthly confirmation. This mirrors the triple-MACD alignment in the MQL version.
3. At least one of the last three momentum readings on the trend timeframe deviates from 100 by the configured minimum (default 0.3). The deviation calculation reproduces the `MathAbs(100 - Momentum)` logic from the EA.
4. No position is currently open.

When the conditions are met a market buy order is placed with the configured volume.

### Short setup
1. MACD line is below its signal line on the execution, trend, and monthly timeframes.
2. At least one of the last three momentum deviations on the trend timeframe exceeds the configured short threshold.
3. No position is currently open (the port avoids hedging and scaling).

If all rules hold a market sell order is submitted.

### Trade management
- The strategy optionally starts protective orders using point-based distances for both stop-loss and take-profit. These distances are multiplied by the security price step to convert points to price increments.
- No trailing-stop, breakeven, or equity-based protective logic from the original EA is included; StockSharp protection is applied once at startup.
- Signals are evaluated only on finished candles to avoid intra-bar noise.

## Multi-timeframe data
- **Primary timeframe**: execution frequency (default 15 minutes). MACD and the pair of LWMAs are calculated here.
- **Trend timeframe**: higher timeframe confirmation (default 1 hour). Both MACD and momentum run on this subscription. Momentum deviations are collected from the latest three closed candles.
- **Monthly timeframe**: long-term MACD confirmation (default 30 days to approximate a calendar month).

The strategy overrides `GetWorkingSecurities` so that all three subscriptions are requested from the connector up-front.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `OrderVolume` | Trade volume in lots. Must be positive. | `0.1` |
| `TakeProfitPoints` | Take-profit distance measured in points. Set to zero to disable. | `50` |
| `StopLossPoints` | Stop-loss distance in points. Set to zero to disable. | `20` |
| `FastMaPeriod` | Fast LWMA length on the primary timeframe. | `6` |
| `SlowMaPeriod` | Slow LWMA length on the primary timeframe. | `85` |
| `MacdFastPeriod` | Fast EMA period used by every MACD instance. | `12` |
| `MacdSlowPeriod` | Slow EMA period used by every MACD instance. | `26` |
| `MacdSignalPeriod` | Signal EMA period for MACD. | `9` |
| `MomentumPeriod` | Momentum lookback on the trend timeframe. | `14` |
| `MomentumBuyThreshold` | Minimum absolute deviation from 100 required for long trades. | `0.3` |
| `MomentumSellThreshold` | Minimum absolute deviation from 100 required for short trades. | `0.3` |
| `PrimaryCandleType` | Candle type for execution. Defaults to 15-minute time frame. | `15m` |
| `TrendCandleType` | Candle type for confirmation. Defaults to 1-hour time frame. | `1h` |
| `MonthlyCandleType` | Candle type for long-term confirmation. Defaults to a 30-day bar. | `30d` |

## Usage notes
- The LWMA filter is intentionally asymmetric: only long trades require the fast LWMA to be below the slow LWMA, matching the behaviour observed in the MQL script.
- Because the port trades a single net position, it skips the martingale-style position sizing present in the source code (`LotsOptimized`). If stacking is required it can be reintroduced by tracking filled volume and comparing it with `OrderVolume`.
- Ensure that the connected broker or data source can provide all three candle timeframes; otherwise, the strategy will remain idle waiting for indicator formation.
- Consider adjusting the monthly timeframe for markets where 30-day candles are unavailable by supplying a custom `DataType` parameter.
- The strategy operates entirely on closed candles and does not read historical indicator buffers directly, complying with the StockSharp indicator usage guidelines.

## Differences vs. the original EA
- Trailing-stop, breakeven, money-based exits, and account-wide equity protection are not ported. StockSharp protection with static distances is used instead.
- Order pyramiding and martingale logic are omitted for clarity. Position sizing remains constant.
- Notifications (alerts, e-mails, push messages) are not implemented.

## Disclaimer
Algorithmic trading involves significant financial risk. Test the strategy on historical data and in a simulated environment before deploying it with real capital.
