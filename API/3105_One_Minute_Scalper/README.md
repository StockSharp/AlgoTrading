[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **1 MINUTE SCALPER** MetaTrader 4 expert advisor into the StockSharp high-level API. It keeps the multi-layer trend confirmation, multi-timeframe momentum, and long-term MACD filter of the original robot while adapting risk controls to StockSharp's position-centric model.

## Core Logic

1. **Trend Stack** – thirteen linear weighted moving averages (LWMA 3/5/8/10/12/15/30/35/40/45/50/55/200) must align in strict order. Long trades require each shorter average to print above the next, while shorts invert the condition.
2. **Primary Trend Gate** – an additional fast LWMA (default 6) must stay above the slow LWMA (default 85) for longs and below for shorts, mirroring the EA's fast-vs-slow check.
3. **Candle Structure** – entries only trigger when the overlap patterns from the script are present: for longs the low two bars ago must be below the previous high, for shorts the previous low must dip under the high two bars back.
4. **Momentum Filter** – a 14-period momentum indicator calculated on a higher timeframe (default 15-minute candles) must deviate from 100 by at least the configured thresholds on any of the last three values. This reproduces the `MomLevelB/MomLevelS` comparisons.
5. **Monthly MACD Bias** – a MACD built on the selected MACD timeframe (defaults to 30-day candles as a proxy for monthly data) must show the main line above the signal line for longs or below for shorts.

## Trade Management

- **Initial Protection** – stop-loss and take-profit distances are expressed in instrument steps (points). When a position opens, the strategy converts these step counts to absolute prices using `Security.PriceStep`.
- **Break-Even Move** – after price moves by `BreakEvenTriggerSteps` in favor, the stop is moved to the entry plus `BreakEvenOffsetSteps` (for shorts the mirrored logic applies). The flag is triggered once per position.
- **Step Trailing** – when `TrailingStopSteps` is positive the stop follows the highest (or lowest) price since entry by the specified number of steps.
- **Money Trailing** – once floating profit exceeds `MoneyTrailTarget` (currency), the strategy tracks the peak floating PnL and closes the position if the pullback equals `MoneyTrailStop`.
- **Money/Percent Targets** – optional absolute or percentage take profits close all exposure when the floating PnL crosses the configured thresholds. The percentage target uses the initial portfolio value captured when the strategy starts.
- **Equity Stop** – the strategy monitors the maximum equity (portfolio value plus open PnL). If the drawdown from that peak exceeds `EquityRiskPercent`, all positions are flattened, replicating the EA's `AccountEquityHigh()` safeguard.

## Parameters

| Parameter | Description |
| --- | --- |
| `Volume` | Order volume for new entries. Added to the absolute current position so reversals flip exposure immediately. |
| `FastMaPeriod` / `SlowMaPeriod` | LWMA lengths for the primary trend filter. |
| `MomentumPeriod` | Length of the higher timeframe momentum indicator. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Minimum absolute deviation from 100 required for long/short momentum confirmation. |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD configuration applied to `MacdCandleType`. |
| `StopLossSteps` / `TakeProfitSteps` | Protective stop and target distances measured in price steps. Set to zero to disable. |
| `TrailingStopSteps` | Step-based trailing stop distance (0 disables). |
| `BreakEvenTriggerSteps` / `BreakEvenOffsetSteps` | Distance to trigger the break-even move and the offset applied when moving the stop. |
| `UseMoneyTakeProfit`, `MoneyTakeProfit` | Enable and size the currency-based floating profit target. |
| `UsePercentTakeProfit`, `PercentTakeProfit` | Enable and size the floating profit target as a percentage of initial equity. |
| `EnableMoneyTrailing`, `MoneyTrailTarget`, `MoneyTrailStop` | Configure the floating profit trailing logic. |
| `UseEquityStop`, `EquityRiskPercent` | Enable the drawdown stop and define the maximum drawdown percentage. |
| `CandleType` | Primary working candles (defaults to 1 minute). |
| `MomentumCandleType` | Higher timeframe candles for the momentum indicator (defaults to 15 minutes). |
| `MacdCandleType` | Candles used for the MACD trend filter (defaults to 30 days ≈ monthly). |

## Differences vs. the MT4 Expert

- StockSharp uses net positions, so the strategy always maintains a single aggregated position instead of multiple tickets up to `Max_Trades`. Reversals close the existing exposure before opening in the opposite direction.
- `PercentTakeProfit` references the portfolio value captured at start instead of the constantly changing `AccountBalance()` used by MetaTrader, which avoids noisy targets when external trades modify the balance.
- Money-based exit logic (`Take_Profit_In_Money` and `TRAIL_PROFIT_IN_MONEY2`) operates on the live floating PnL calculated from the strategy's average entry price. This matches the EA's behaviour but inside StockSharp's protection framework.
- The platform must supply candle feeds for the selected timeframes (`CandleType`, `MomentumCandleType`, `MacdCandleType`). Ensure the adapters you use support the requested resolutions.

Tune the thresholds to fit your instrument and session. Narrow spreads or highly volatile pairs may require wider step distances or larger momentum thresholds to reduce noise.
