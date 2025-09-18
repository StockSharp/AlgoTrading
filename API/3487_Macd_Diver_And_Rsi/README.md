# MACD Diver and RSI Strategy

## Overview

This strategy is a C# conversion of the **"Macd diver and rsi"** MetaTrader 5 expert advisor. It keeps the original two-stage signal idea: the Relative Strength Index (RSI) detects oversold or overbought extremes, while the MACD histogram confirms that momentum is turning back in the direction of the trade. Long and short sides are configured independently so the behaviour can be tuned for bullish and bearish setups separately.

The strategy operates on a single candle subscription (configurable timeframe) and trades the charted security directly through market orders. All indicator processing uses the high-level StockSharp API via `BindEx`, matching the project rules.

## Trading Logic

1. **Indicator preparation**
   - Two RSI indicators are created, one for the long leg and one for the short leg, with individual lengths and thresholds.
   - Two `MovingAverageConvergenceDivergenceSignal` indicators mirror the MACD settings for long and short trades. Their histogram component is used to confirm momentum reversals.

2. **Entry rules**
   - **Long setup**: when the long RSI value is at or below the oversold threshold *and* the long MACD histogram crosses above zero (changes sign from negative to positive), a bullish position is opened. If a short position is active it is closed and reversed in the same market order.
   - **Short setup**: when the short RSI value is at or above the overbought threshold *and* the short MACD histogram crosses below zero, a bearish position is opened. Existing long exposure is flattened before the new short is established.

3. **Risk management**
   - After each entry the strategy records the close price of the signal bar as the reference price.
   - Stop-loss and take-profit levels are projected from that price using pip distances defined separately for long and short trades.
   - Pips are converted to price units with the instrument `PriceStep`, and automatically scaled by 10 for symbols with 3 or 5 decimals to mirror MT5 behaviour.
   - On every completed candle the high/low range is checked against these levels. Hitting either level immediately closes the position with a market order.

4. **Trade management**
   - The position state is cleared whenever the position size returns to zero (either because a stop/take-profit was reached or the strategy was reversed by an opposite signal).
   - No partial exits or trailing adjustments are performed; the position is managed only via the static stop-loss and take-profit levels.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe of the candle subscription used for signals. |
| `LongRsiPeriod`, `ShortRsiPeriod` | RSI lengths for long and short detection. |
| `LongRsiThreshold`, `ShortRsiThreshold` | RSI thresholds that enable entries (oversold for longs, overbought for shorts). |
| `LongMacdFastLength`, `LongMacdSlowLength`, `LongMacdSignalLength` | MACD EMA lengths for the bullish leg. |
| `ShortMacdFastLength`, `ShortMacdSlowLength`, `ShortMacdSignalLength` | MACD EMA lengths for the bearish leg. |
| `LongVolume`, `ShortVolume` | Trade volume per signal. When reversing, the strategy adds the absolute open volume so the single order performs the close and new open. |
| `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Distance of stop-loss and take-profit orders in pips. Zero disables the respective level. |

## Notes

- The strategy requires instruments with a non-zero `PriceStep`. If the step is missing the pip calculation falls back to 0.0001 to prevent division-by-zero.
- Because both sides use independent indicator instances, you can tune bullish and bearish behaviour separately, for example by tightening the overbought threshold while keeping the oversold side more permissive.
- The code adds English comments and documentation to clarify the trading process and satisfy the project guidelines.
