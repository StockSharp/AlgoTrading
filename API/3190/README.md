# Ichimoku Momentum MACD Strategy

## Summary
- **Type**: Trend following with momentum confirmation.
- **Timeframe**: Configurable (default 15-minute candles).
- **Indicators**: Ichimoku (Tenkan/Kijun), Linear Weighted Moving Averages, Momentum, MACD.
- **Stops**: Optional fixed take-profit and stop-loss in price points via `StartProtection`.

## Strategy Description
This strategy recreates the decision flow of the MetaTrader expert "Ichimoku" (folder `MQL/23469`). It evaluates the previous
closed candle and opens new trades at the start of the next bar when all four confirmations agree:

1. **Ichimoku alignment** – Tenkan (conversion line) must be above Kijun (base line) for long trades and below it for shorts.
2. **LWMA trend filter** – A fast linear weighted moving average must stay above the slow LWMA for longs and below it for
   shorts. Both averages are calculated on the same timeframe as the subscribed candles.
3. **Momentum strength** – The absolute distance of the momentum oscillator from the neutral level 100 has to be greater than a
   configurable threshold on at least one of the last three closed candles.
4. **MACD confirmation** – The MACD histogram must agree with the direction (MACD line positioned beyond the signal line with the
   same sign).

When all four conditions line up bullishly and the strategy is not currently long, it buys the configured volume plus any units
required to flatten an existing short position. When the conditions flip to bearish it mirrors the process on the sell side.
Opposite signals always close open positions, providing a deterministic exit even without protective orders.

Risk management is handled through StockSharp's `StartProtection`, allowing fixed take-profit and stop-loss distances expressed
in instrument points. Setting either parameter to zero disables the corresponding protection leg.

## Parameter Overview
| Parameter | Description |
|-----------|-------------|
| `FastMaPeriod` | Length of the fast linear weighted moving average used for the trend filter. |
| `SlowMaPeriod` | Length of the slow linear weighted moving average. |
| `MomentumPeriod` | Lookback period of the momentum oscillator. |
| `MomentumThreshold` | Minimum distance from 100 that the momentum must achieve on at least one of the last three candles. |
| `MacdFastPeriod` | Fast EMA length of the MACD filter. |
| `MacdSlowPeriod` | Slow EMA length of the MACD filter. |
| `MacdSignalPeriod` | Signal EMA length of the MACD filter. |
| `TenkanPeriod` | Ichimoku Tenkan-sen length. |
| `KijunPeriod` | Ichimoku Kijun-sen length. |
| `SenkouSpanBPeriod` | Ichimoku Senkou Span B length. |
| `TakeProfitPoints` | Optional take-profit distance in price points (0 disables). |
| `StopLossPoints` | Optional stop-loss distance in price points (0 disables). |
| `CandleType` | Timeframe used for all indicator calculations. |

## Usage Notes
- The strategy reads only finished candles and stores the indicator values of the previous bar, matching the MetaTrader EA's
  `shift=1` logic.
- Adjust `MomentumThreshold` when switching to markets with different momentum scaling (e.g., crypto vs. forex pairs).
- Protective orders are managed internally; exchange-level bracket orders are not submitted.
- Charts, if available, will display price candles, both LWMAs, the Ichimoku cloud, and executed trades.
