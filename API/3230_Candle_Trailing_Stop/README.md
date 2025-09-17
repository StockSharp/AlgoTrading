# Candle Trailing Stop Strategy

The **Candle Trailing Stop** strategy is a StockSharp port of the MetaTrader expert advisor with the same name. The original robot
combined multi-timeframe trend filters, momentum confirmation and an aggressive trailing stop engine that followed the lows and
highs of recent candles. The C# version keeps the same workflow but relies on high-level StockSharp components and exposes all
critical settings as strategy parameters.

## Core logic

1. **Data subscriptions**
   - The trading timeframe drives entries and trailing stop updates.
   - A higher timeframe provides confirmation using linear weighted moving averages (LWMA) and a momentum indicator.
   - A third subscription calculates a MACD line on a slow timeframe (monthly by default) to filter trades.
2. **Trend alignment**
   - Trades are allowed only when the fast, middle and slow LWMA sequences are aligned on both the trading and higher
     timeframes (bullish sequence for longs, bearish for shorts).
3. **Momentum gate**
   - The momentum indicator must be close to the neutral value of 100 for at least one of the last three higher-timeframe bars.
4. **MACD confirmation**
   - Longs require the MACD line to be above the signal line; shorts require the opposite relationship.
5. **Entry trigger**
   - A breakout through the fast LWMA on the current timeframe (candle closing above/below the average after touching it on the
     previous bar) initiates new trades while respecting a configurable position limit.
6. **Risk and exit management**
   - Initial stop-loss and take-profit distances are defined in pips and automatically converted to price steps.
   - Stops can migrate to break-even, trail behind the extreme of recent candles, or fall back to a classic fixed-distance trail.
   - Optional equity-based features mirror the original EA: monetary take profit, percentage take profit, equity trailing and
     drawdown protection.

## Parameters

| Group        | Name                    | Description                                                                                 | Default |
|--------------|-------------------------|---------------------------------------------------------------------------------------------|---------|
| Trading      | `Volume`                | Order size in lots/contracts.                                                               | `1`     |
|              | `MaxTrades`             | Maximum aggregated exposure expressed as `Volume * MaxTrades`.                              | `10`    |
| Indicators   | `FastCurrentLength`     | Fast LWMA on the trading timeframe.                                                         | `9`     |
|              | `MiddleCurrentLength`   | Middle LWMA on the trading timeframe.                                                       | `20`    |
|              | `SlowCurrentLength`     | Slow LWMA on the trading timeframe.                                                         | `52`    |
|              | `FastHigherLength`      | Fast LWMA on the higher timeframe.                                                          | `9`     |
|              | `MiddleHigherLength`    | Middle LWMA on the higher timeframe.                                                        | `20`    |
|              | `SlowHigherLength`      | Slow LWMA on the higher timeframe.                                                          | `52`    |
|              | `MomentumPeriod`        | Higher-timeframe momentum period.                                                           | `14`    |
|              | `MomentumBuyThreshold`  | Maximum deviation from 100 allowed for long trades.                                         | `0.3`   |
|              | `MomentumSellThreshold` | Maximum deviation from 100 allowed for short trades.                                        | `0.3`   |
|              | `MacdFastLength`        | Fast EMA length for MACD confirmation.                                                      | `12`    |
|              | `MacdSlowLength`        | Slow EMA length for MACD confirmation.                                                      | `26`    |
|              | `MacdSignalLength`      | Signal EMA length for MACD confirmation.                                                    | `9`     |
| Risk         | `StopLossPips`          | Stop-loss distance in pips.                                                                 | `20`    |
|              | `TakeProfitPips`        | Take-profit distance in pips.                                                               | `50`    |
|              | `UseMoveToBreakEven`    | Enables the break-even logic.                                                               | `true`  |
|              | `BreakEvenTriggerPips`  | Profit in pips required before moving the stop.                                             | `30`    |
|              | `BreakEvenOffsetPips`   | Offset added when shifting the stop to break even.                                          | `30`    |
|              | `UseCandleTrail`        | Choose between candle-based trailing (`true`) or classic trailing (`false`).                | `true`  |
|              | `CandleTrailLength`     | Number of closed candles used to compute trailing extremes.                                | `3`     |
|              | `PadAmountPips`         | Extra buffer added below/above the trailing extreme.                                        | `10`    |
|              | `TrailTriggerPips`      | Profit required before the classic trail activates.                                         | `40`    |
|              | `TrailAmountPips`       | Distance maintained by the classic trail.                                                   | `40`    |
| Equity rules | `UseMoneyTakeProfit`    | Close all positions when floating profit exceeds the monetary target.                       | `false` |
|              | `MoneyTakeProfit`       | Monetary profit target.                                                                     | `40`    |
|              | `UsePercentTakeProfit`  | Close all positions when floating profit exceeds the percentage target.                     | `false` |
|              | `PercentTakeProfit`     | Percentage of initial equity used as profit target.                                         | `10`    |
|              | `EnableMoneyTrailing`   | Activates trailing of floating profit after a threshold.                                    | `true`  |
|              | `MoneyTrailTarget`      | Profit level that turns on the monetary trailing logic.                                     | `40`    |
|              | `MoneyTrailStop`        | Maximum allowed pullback once the target was reached.                                       | `10`    |
|              | `UseEquityStop`         | Enables equity drawdown protection.                                                         | `true`  |
|              | `EquityRiskPercent`     | Maximum drawdown from the equity peak before forcing a flat position.                       | `1`     |
| Data         | `CurrentCandleType`     | Trading timeframe.                                                                          | `5m`    |
|              | `HigherCandleType`      | Higher timeframe used for filters.                                                          | `30m`   |
|              | `MacdCandleType`        | Timeframe for MACD confirmation (monthly by default).                                       | `30d`   |

## Notes and assumptions

- Pips are converted to price steps using the instrument tick size. On symbols where one pip differs from one tick you may need
  to adjust the default pip distances.
- Monetary features rely on unrealized profit approximated as `(close - averagePrice) * position`. Swap and commission adjustments
  are not simulated.
- The strategy uses market orders for entries and exits. Initial take-profit orders are registered once a trade is opened, while
  stop-loss management is handled internally and exits through market orders when the calculated level is crossed.
- All in-code comments are written in English as requested by the project guidelines.
