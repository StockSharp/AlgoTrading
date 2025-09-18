# Forex Profit System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy reproduces the classic MetaTrader expert advisor "Forex Profit System" inside the StockSharp high-level API. It uses
three exponential moving averages (EMA 10, 25 and 50) on the candle median price combined with a Parabolic SAR filter. The
combination detects short-lived momentum bursts that appear after the fast average crosses the slow trend line while the Parabolic
SAR already flipped to the same side as price.

## Trading Logic

1. **Indicator stack**
   - Median price derived from the finished candle drives all indicators so that results match the original MetaTrader "PRICE_MEDIAN"
     input.
   - Fast EMA (length 10) reacts quickly to short-term momentum shifts.
   - Medium EMA (length 25) and slow EMA (length 50) define the directional bias.
   - Parabolic SAR with step 0.02 and maximum 0.2 confirms that price already broke to the new side of the trend.
2. **Long entry**
   - EMA(10) is greater than both EMA(25) and EMA(50).
   - EMA(10) was below EMA(50) on the previous closed candle (cross-up confirmation).
   - Parabolic SAR value is below the candle close, meaning the dots switched to bullish mode.
   - No open position exists and the strategy is allowed to trade (online + permissions).
3. **Short entry**
   - EMA(10) is lower than both EMA(25) and EMA(50).
   - EMA(10) was above EMA(50) on the previous closed candle (cross-down confirmation).
   - Parabolic SAR is above the candle close.
4. **Exit management**
   - Hard stop-loss and take-profit are applied immediately after entry with asymmetric settings for long and short trades.
   - A trailing stop is armed once price moves far enough in favor of the position. The stop is pulled to `current price -/+ trailing`
     distance depending on the direction.
   - Early exit occurs when EMA(10) reverses direction (drops below its previous value for longs or rises above for shorts) and the
     open profit exceeds a minimum trigger distance.

## Default Parameter Values

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 15 minute time frame | Candle series processed by the strategy. |
| `FastEmaLength` | 10 | Period of the fast EMA. |
| `MediumEmaLength` | 25 | Period of the medium EMA. |
| `SlowEmaLength` | 50 | Period of the slow EMA. |
| `SarStep` | 0.02 | Initial acceleration for Parabolic SAR. |
| `SarMax` | 0.2 | Maximum acceleration for Parabolic SAR. |
| `Volume` | 0.1 | Trading volume in lots/contracts. |
| `LongTakeProfitPoints` | 50 | Take-profit distance for long trades measured in price points. |
| `ShortTakeProfitPoints` | 50 | Take-profit distance for short trades measured in price points. |
| `LongStopLossPoints` | 30 | Stop-loss distance for long trades measured in price points. |
| `ShortStopLossPoints` | 30 | Stop-loss distance for short trades measured in price points. |
| `LongTrailingStopPoints` | 10 | Trailing stop trigger distance for long trades. |
| `ShortTrailingStopPoints` | 10 | Trailing stop trigger distance for short trades. |
| `LongProfitTriggerPoints` | 10 | Minimum open profit (points) required before a long trade can be closed on EMA reversal. |
| `ShortProfitTriggerPoints` | 5 | Minimum open profit (points) required before a short trade can be closed on EMA reversal. |

## Implementation Notes

- The strategy uses candle subscriptions and indicator binding in the high-level API while keeping all risk control inside the
  strategy class. No low-level order book access is required.
- All trade management distances are converted from points into actual price offsets using the instrument `PriceStep`. If `PriceStep`
  is not available the raw point value is used so the algorithm still functions on synthetic instruments.
- Protective stops (`SetStopLoss`, `SetTakeProfit`) are set using the resulting position after the market order is sent to stay in
  sync with potential partial fills.
- Internal state keeps track of the last entry price per direction so that trailing and EMA-based exits can evaluate the realized
  progress precisely.
- Because all logic runs on finished candles, there is no repainting risk and signals mirror the original MetaTrader behavior that
  calculated everything on `start()` close prices.

## Suggested Usage

- The method is suited for liquid FX pairs on intraday charts (15-minute default). Higher time frames can be used by adjusting the
  EMA periods and trade management distances accordingly.
- For assets with different tick sizes or volatility levels adjust the point-based parameters (`StopLoss`, `TakeProfit`,
  `TrailingStop`, `ProfitTrigger`) so that distances match the instrument profile.
- Combine with spread or session filters if the venue has wide spreads during certain hours; the strategy expects reasonable
  execution to realize the short-term momentum bursts.
