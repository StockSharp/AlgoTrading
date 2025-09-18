# True Scalper Profit Lock Strategy

## Overview

The True Scalper Profit Lock strategy is a conversion of the MetaTrader 4 expert advisor **TrueScalperProfitLock.mq4**. It combines a short-term exponential moving average crossover with RSI-based polarity filters to time entries. The strategy is designed for high-frequency scalping environments where positions are actively managed using a protective stop, a fixed take-profit level, and an optional break-even lock.

## Trading Logic

- **Trend filter:** A 3-period EMA calculated on the previous closed candle must trade above (for buys) or below (for sells) a 7-period EMA from the same bar. The distance between the averages has to exceed one price step to avoid flat market conditions.
- **RSI confirmation:** The original EA offers two validation modes. Method A waits for the 2-period RSI to cross the configured threshold between the two most recent closed candles. Method B simply checks whether the RSI from two candles ago is above or below the threshold. Both modes can be used independently or together, with Method B enabled by default.
- **Order direction:** Long trades require the fast EMA to be above the slow EMA while the RSI indicates oversold conditions (`RSI < threshold`). Short trades mirror the logic and expect overbought readings.

## Position Management

- **Initial protection:** Upon entry the strategy computes a fixed-distance stop-loss and take-profit using the security price step. Both parameters follow the default values from the MetaTrader version (90 and 44 points respectively).
- **Profit lock:** When enabled, the stop-loss is moved to break-even plus a configurable offset once price advances by the `BreakEvenTriggerPoints` distance. This mirrors the "ProfitLock" behaviour from the original EA.
- **Abandon timers:** Two optional mechanisms close trades after a predefined number of completed candles (`AbandonBars`). Method A reverses the position immediately by setting an opposite entry flag, while Method B just closes and waits for fresh indicator signals.
- **Money management:** The lot sizing formula matches the original script: position size is derived from the portfolio balance, risk percentage, account type (mini vs. standard), and live-trading bounds. Setting `UseMoneyManagement` to `false` reverts to the fixed volume parameter.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe of the processed candles. |
| `FixedVolume` | Base order volume when money management is disabled. |
| `TakeProfitPoints` / `StopLossPoints` | Profit target and protective stop in price steps. |
| `UseRsiMethodA` / `UseRsiMethodB` | Enable RSI confirmation methods matching the EA. |
| `RsiThreshold` | RSI level used by both confirmation modes. |
| `AbandonMethodA` / `AbandonMethodB` | Enable the abandon logic variants. |
| `AbandonBars` | Number of completed candles before the abandon logic triggers. |
| `UseMoneyManagement`, `RiskPercent`, `AccountIsMini`, `LiveTradingMode` | Volume calculation controls. |
| `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` | Break-even activation and offset. |
| `MaxOpenTrades` | Maximum number of concurrent trades (default behaviour is one open position). |

## Usage Notes

1. The strategy only evaluates completed candles to stay consistent with the MetaTrader expert, which relies on bar `shift` lookbacks.
2. Enable or disable RSI methods to reproduce the exact configuration used in the original template.
3. Break-even and abandon logic rely on candle highs/lows to detect price hits. When running on higher timeframes consider the potential for intra-bar overshoots.
4. Money management requires a portfolio connection supplying the `BeginValue`. If unavailable, the strategy falls back to the fixed volume.

## Files

- `CS/TrueScalperProfitLockStrategy.cs` – C# implementation of the strategy.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.

