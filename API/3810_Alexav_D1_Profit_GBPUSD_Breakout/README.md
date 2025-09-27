# Alexav D1 Profit GBPUSD Strategy

## Overview
Alexav D1 Profit GBPUSD is a daily breakout system converted from the MetaTrader 4 expert advisor *Alexav_d1_profit_gbpusd.mq4*. The strategy operates on daily candles of GBP/USD and evaluates the completed session once per day (Tuesday through Friday). Momentum confirmation is provided by RSI and MACD while volatility-adjusted stops and staggered profit targets are derived from ATR.

## Trading Logic
1. **Indicator Preparation**
   - Two EMAs with the same period are applied to the daily high and low prices to define bullish and bearish reference levels.
   - RSI with a 10-period lookback measures momentum. Extreme RSI readings temporarily block new trades in that direction.
   - MACD (5/24/14) delivers an acceleration filter by comparing the last two histogram values.
   - ATR (28) provides the volatility unit used for stops and profit targets.
2. **Session Filter**
   - Only one evaluation is performed for each completed daily candle from Tuesday to Friday. Mondays and weekends are skipped.
3. **Long Setup**
   - The previous daily candle must close above the EMA of highs calculated two sessions ago.
   - RSI of the previous session must be above the upper level (default 60) but below the upper limit (default 80).
   - MACD must either be below zero two sessions ago or show a sufficient positive acceleration compared with the prior value.
   - If the previous open falls back below the EMA of highs, the strategy allows a new batch of buys after the block resets.
4. **Short Setup**
   - Mirror logic of the long setup, using the EMA of lows, RSI lower thresholds (39 / 25), and MACD filters.

## Order Management
When a setup is confirmed the strategy opens a batch of four market orders (each using the strategy `Volume`):
- **Stops**: Each order shares the same protective stop equal to `ATR * AtrStopMultiplier` (default 1.6) away from the entry price.
- **Targets**: Profit objectives scale by `AtrTargetMultiplier * (1 + i / 2)` for order index `i` in `[0..3]`, replicating the original EA’s 1.0, 1.5, 2.0, and 2.5 ATR offsets.
- **Conflict Handling**: Opposite positions are flattened before opening a new batch. Triggering a long batch clears any pending short batch (and vice versa).

The strategy monitors completed candles. If the daily low touches the stop, the corresponding long order is closed at market; if the high reaches the target, the order is also closed. Shorts are handled symmetrically using the candle high for stops and the low for targets.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Primary candle series, daily by default. | 1 day |
| `MaPeriod` | Period of the EMA applied to highs/lows. | 6 |
| `RsiPeriod` | RSI period for momentum filter. | 10 |
| `AtrPeriod` | ATR period for stop/target sizing. | 28 |
| `AtrStopMultiplier` | ATR multiple for stops. | 1.6 |
| `AtrTargetMultiplier` | Base ATR multiple for targets. | 1.0 |
| `RsiUpperLevel` | RSI threshold confirming bullish momentum. | 60 |
| `RsiUpperLimit` | RSI cap that blocks new longs. | 80 |
| `RsiLowerLevel` | RSI threshold confirming bearish momentum. | 39 |
| `RsiLowerLimit` | RSI floor that blocks new shorts. | 25 |
| `FastMaPeriod` | Fast EMA period for MACD. | 5 |
| `SlowMaPeriod` | Slow EMA period for MACD. | 24 |
| `SignalMaPeriod` | Signal EMA period for MACD. | 14 |
| `MacdDiffBuy` | Minimum MACD acceleration for longs. | 0.5 |
| `MacdDiffSell` | Minimum MACD acceleration for shorts. | 0.15 |

Set the strategy `Volume` to the desired lot size per order before starting the strategy.

## Notes
- The conversion keeps the single-evaluation-per-day logic found in the original expert advisor.
- Use historical daily data for GBP/USD when backtesting to reproduce the intended behaviour.
- Protective stops and targets are simulated using completed candle extremes; intraday spikes inside a daily candle are not visible to the strategy.
