# Consolidation Breakout Strategy

This strategy reproduces the core behaviour of the original **Consolidation Breakout** expert advisor for MetaTrader. It looks for tight consolidations confirmed by momentum and MACD filters, then opens a position in the direction of the breakout. Risk is managed through fixed take-profit and stop-loss distances measured in price steps (pips).

## How it works

1. The primary timeframe is defined by the `CandleType` parameter. All trend and consolidation checks are evaluated on these candles.
2. Two linear weighted moving averages (LWMAs) computed on the typical price provide the directional filter. Long setups require the fast LWMA to stay above the slow LWMA, while short setups need the opposite alignment.
3. A consolidation is detected when the low of the candle two bars ago remains below the high of the previous candle (long case) or when the previous low sits below the high from two bars ago (short case). This mirrors the overlapping-bar logic from the MQL version.
4. Momentum must confirm the move. The absolute momentum value (relative to zero) needs to exceed the respective buy or sell threshold. This approximates the original expert's momentum filter around the 100 level.
5. A separate MACD calculated on the `MacdCandleType` timeframe must agree with the trade direction. The strategy checks whether the MACD line leads the signal line on both the positive and negative sides of the axis, reproducing the multi-timeframe confirmation from the source code.
6. When all filters align and the account is flat or positioned in the opposite direction, the strategy submits a market order sized by `TradeVolume`. Protective levels are immediately recalculated in price steps so that intrabar extremes can trigger exits.
7. Every finished candle also monitors active positions. If the candle range touches either the stop-loss or the take-profit level, the strategy closes the position at market and resets the protection targets.

## Indicators

- Linear Weighted Moving Average (fast and slow, typical price)
- Momentum
- MACD (with 12/26/9 periods on a higher timeframe)

## Parameters

- `CandleType` – primary timeframe used for breakout detection.
- `MacdCandleType` – timeframe used for the confirming MACD filter.
- `FastMaPeriod` – length of the fast LWMA.
- `SlowMaPeriod` – length of the slow LWMA.
- `MomentumLength` – lookback for the momentum filter.
- `MomentumBuyThreshold` – minimum positive momentum required for long trades.
- `MomentumSellThreshold` – minimum negative momentum required for short trades (expressed as absolute value).
- `StopLossPips` – protective stop distance in price steps.
- `TakeProfitPips` – profit target distance in price steps.
- `TradeVolume` – volume submitted with each market order.

The defaults mirror the published expert advisor: LWMA periods of 6 and 85, momentum length 14, buy/sell thresholds of 0.3, stop-loss of 20 pips, and take-profit of 50 pips. Adjust the pip-based distances when trading instruments with different price steps.

## Notes

- Trailing stops, break-even moves, and money-management modules from the MQL script are intentionally omitted to keep the StockSharp port focused on the core breakout logic.
- Always ensure the selected timeframes are supported by your data feed. If the higher timeframe produces sparse data, consider switching to a lower `MacdCandleType` to keep the MACD filter responsive.
