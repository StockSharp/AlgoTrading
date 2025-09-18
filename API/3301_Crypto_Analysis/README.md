# Crypto Analysis Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor "Crypto Analysis". It hunts for breakouts that occur after price tags the outer Bollinger Band on the main trading timeframe while the market structure remains bearish (fast LWMA below the slow LWMA). The system only allows trades when a higher timeframe momentum burst and a monthly MACD filter both agree with the desired direction. Once in the market, the position is managed by a layered protection block that mirrors the original EA: pip-based stops, money-based trailing, break-even relocation, and portfolio drawdown controls.

## Trading logic
- **Signal timeframe:** configurable (default M15). All entry/exit rules are evaluated on these candles.
- **Volatility trigger:** previous candle low must touch or pierce the lower Bollinger Band (20, 2) to prepare a long setup, while a touch of the upper band prepares a short setup.
- **Trend filter:** both scenarios require the fast linear weighted moving average (LWMA, default 6) to stay below the slow LWMA (default 85), replicating the bearish bias check in the EA.
- **RSI confirmation:** RSI(14) has to be above 50 for longs and below 50 for shorts.
- **Momentum burst:** the maximum absolute deviation of the last three higher-timeframe Momentum(14) values from the 100 baseline must exceed the buy/sell thresholds. This captures the momentum spikes used by the MQL code.
- **Monthly MACD filter:** a separate monthly (default 30-day candles) MACD (12, 26, 9) confirms direction; longs require MACD main above signal, shorts demand the opposite.
- **Entry execution:** once all filters align, the strategy opens a market order. Opposite positions are flattened before reversing to keep a single net position, which mirrors the EA’s behaviour of closing opposing trades.

## Position management
- **Initial stop and target:** configurable distances in pips are converted from the instrument tick size using the same 5-digit/3-digit handling as the EA (`0.00001` and `0.001` steps are multiplied by 10).
- **Trailing stop:** after a new high (long) or low (short) forms, the stop is pulled behind price by `TrailingStopPips`, always respecting the best level achieved.
- **Break-even:** when profit reaches `BreakEvenTriggerPips`, the stop is moved to the entry price plus `BreakEvenOffsetPips` (long) or minus the offset (short).
- **Money targets:** optional absolute or percentage-based profit caps close the position as soon as the floating PnL hits the requested level.
- **Money trailing:** once unrealized profit exceeds `MoneyTrailTarget`, the strategy tracks the peak and closes the position if the giveback equals or exceeds `MoneyTrailStop`.
- **Equity stop:** the floating equity (current portfolio value plus unrealized PnL) is monitored; if the drawdown from the peak surpasses `EquityRiskPercent`, the position is flattened.

## Multi-timeframe data
Three subscriptions are registered automatically:
1. Primary candle series for the Bollinger/LWMA/RSI rules.
2. Higher timeframe candles for the momentum filter (default H1).
3. Monthly candles for the MACD confirmation (default 30-day bars).

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Base order size. Opposite positions are closed before opening a new one. |
| `UseMoneyTakeProfit` | Enable the absolute monetary take-profit target. |
| `MoneyTakeProfit` | Profit in portfolio currency that triggers an exit when `UseMoneyTakeProfit` is true. |
| `UsePercentTakeProfit` | Enable the percent-based take-profit target calculated from the initial equity. |
| `PercentTakeProfit` | Profit percentage required to close the position when the percent target is active. |
| `EnableMoneyTrailing` | Activates the money-based trailing block. |
| `MoneyTrailTarget` | Profit level that starts the money trail. |
| `MoneyTrailStop` | Maximum allowed profit giveback after `MoneyTrailTarget` has been reached. |
| `StopLossPips` | Initial stop-loss distance in pips. |
| `TakeProfitPips` | Initial take-profit distance in pips. |
| `TrailingStopPips` | Trailing stop distance in pips. |
| `UseBreakEven` | Enable the break-even stop relocation. |
| `BreakEvenTriggerPips` | Pip profit required before break-even protection activates. |
| `BreakEvenOffsetPips` | Additional pips added to the entry price when placing the break-even stop. |
| `FastMaPeriod` | Length of the fast LWMA calculated on typical price. |
| `SlowMaPeriod` | Length of the slow LWMA calculated on typical price. |
| `MomentumPeriod` | Period of the Momentum indicator on the higher timeframe. |
| `MomentumBuyThreshold` | Minimum momentum deviation for long entries. |
| `MomentumSellThreshold` | Minimum momentum deviation for short entries. |
| `MacdFastLength` | Fast EMA length for the higher timeframe MACD filter. |
| `MacdSlowLength` | Slow EMA length for the higher timeframe MACD filter. |
| `MacdSignalLength` | Signal length for the higher timeframe MACD filter. |
| `UseEquityStop` | Enable portfolio drawdown protection. |
| `EquityRiskPercent` | Allowed equity drawdown percentage before forcefully closing the position. |
| `CandleType` | Primary timeframe used for entries. |
| `MomentumCandleType` | Higher timeframe used for momentum confirmation. |
| `MacdCandleType` | Higher timeframe used for MACD confirmation. |

## Notes
- The StockSharp port keeps a single net position, matching the EA which closes opposite orders before opening a new trade.
- All protective rules operate on closed candles to replicate the "new bar" processing in the original script.
- When using synthetic symbols or instruments without a standard pip size, adjust `StopLossPips` and related parameters to the exchange’s tick value.
