# MACD and SAR
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the original MetaTrader "MACD and SAR" expert advisor. It evaluates the relationship between the MACD main and signal lines together with the Parabolic SAR level on every completed candle. Configurable switches allow you to invert each comparison so that the same template can be used for counter-trend or trend-following setups. Multiple entries are allowed as long as the configured maximum number of stacked positions is not exceeded.

When a long signal appears, existing short exposure is flattened and a new long lot is opened (if the cap is not reached). Likewise, a short signal closes longs first and then adds a short lot. There are no additional stop-loss or take-profit orders; trades are closed only when the opposite signal is generated.

## Strategy logic

1. Wait for a finished candle from the configured timeframe.
2. Read MACD values (main, signal, histogram) and the Parabolic SAR level calculated on close prices.
3. Evaluate the following comparisons, each of which can be flipped by its corresponding boolean parameter:
   - MACD main line vs. signal line.
   - MACD signal line vs. the zero level.
   - Parabolic SAR vs. closing price.
4. If all three comparisons for the long side are satisfied and the strategy still has room to stack new positions, buy the specified lot size (including any volume required to close shorts).
5. If all three comparisons for the short side are satisfied and the stacking limit allows it, sell the specified lot size (including any volume required to close longs).

## Parameters

- `TradeVolume` — volume per individual trade (default `0.1`).
- `MaxPositions` — maximum number of stacked positions in one direction (default `10`).
- `MacdFastPeriod` — fast EMA period for MACD (default `12`).
- `MacdSlowPeriod` — slow EMA period for MACD (default `26`).
- `MacdSignalPeriod` — signal smoothing period for MACD (default `9`).
- `SarStep` — Parabolic SAR acceleration step (default `0.02`).
- `SarMaximum` — Parabolic SAR maximum acceleration (default `0.2`).
- `BuyMacdGreaterSignal` — if `true`, require MACD main > signal for longs; otherwise expect the opposite (default `true`).
- `BuySignalPositive` — if `true`, require MACD signal > 0 for longs; otherwise expect signal < 0 (default `false`).
- `BuySarAbovePrice` — if `true`, require SAR above price for longs; otherwise expect price above SAR (default `false`).
- `SellMacdGreaterSignal` — if `true`, require MACD main > signal for shorts; otherwise expect MACD main < signal (default `false`).
- `SellSignalPositive` — if `true`, require MACD signal > 0 for shorts; otherwise expect signal < 0 (default `true`).
- `SellSarAbovePrice` — if `true`, require SAR above price for shorts; otherwise expect price above SAR (default `true`).
- `CandleType` — candle type/timeframe used for data processing (default `15` minutes).

## Additional notes

- The strategy relies solely on indicator crossings; there are no protective stops or profit targets.
- Position stacking is implemented by checking the absolute position volume against `MaxPositions * TradeVolume` with a small tolerance to handle rounding.
- All trades are executed with market orders. Make sure the portfolio volume setting matches the instruments you plan to trade.
- Add optional portfolio protection rules if you need drawdown limits or trailing stops; none are included by default.

