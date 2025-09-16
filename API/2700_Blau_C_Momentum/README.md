# Blau C-Momentum Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor **Exp_BlauCMomentum**. It trades on a single instrument using candles from a configurable timeframe and interprets Blau's triple-smoothed momentum in one of two modes:

* **Breakdown mode** – reacts to the momentum line crossing the zero level.
* **Twist mode** – reacts to changes in the direction of the smoothed momentum slope.

The indicator is calculated on an external timeframe and can optionally use different applied prices for the momentum calculation. Positions are opened with market orders and can be protected using built-in stop-loss and take-profit modules.

## How it works
1. Subscribe to candles of the selected timeframe.
2. Compute Blau C-Momentum:
   * Raw momentum is the difference between two applied prices separated by `MomentumLength` bars.
   * The raw momentum is smoothed three times by the chosen moving-average method and scaled to price steps (×100/Point).
3. Store the smoothed indicator history for bar shifts defined by `SignalBar`.
4. Generate signals:
   * **Breakdown** – if the previous bar was above zero and the signal bar is below or equal to zero, open/flip long; if the previous bar was below zero and the signal bar is above or equal to zero, open/flip short. Optional exit flags close the opposite side when the previous bar crosses the zero line.
   * **Twist** – compare two previous bars; when momentum accelerates upward (previous &lt; older) and the signal bar confirms, open/flip long; when momentum accelerates downward (previous &gt; older) and the signal bar confirms, open/flip short. Optional exit flags close the opposite side on the same condition.
5. Use `MoneyManagement` and `MarginMode` to size the position. Negative values mean fixed volume; positive values risk or allocate a fraction of the portfolio value. A simple time lock prevents immediate re-entries within the same candle.

## Parameters
| Group | Name | Description |
|-------|------|-------------|
| Trading | `MoneyManagement` | Share of capital for position sizing. Negative value = fixed volume. |
| Trading | `MarginMode` | Interpretation of money management (`FreeMarginShare`, `BalanceShare`, `FreeMarginRisk`, `BalanceRisk`). Risk modes use stop-loss distance and `StepPrice`. |
| Risk | `StopLossPoints` | Stop-loss distance in instrument price steps (set `0` to disable). |
| Risk | `TakeProfitPoints` | Take-profit distance in instrument price steps (set `0` to disable). |
| Trading | `SlippagePoints` | Allowed slippage (kept for compatibility, not used for order placement). |
| Trading | `EnableLongEntry`, `EnableShortEntry` | Allow opening long/short positions. |
| Trading | `EnableLongExit`, `EnableShortExit` | Allow closing existing positions according to the indicator. |
| Logic | `EntryMode` | `Breakdown` or `Twist`. |
| Data | `CandleType` | Timeframe used for indicator calculations (default 4h). |
| Indicator | `SmoothingMethod` | Moving-average method: `Simple`, `Exponential`, `Smoothed`, `LinearWeighted`, `Jurik`, `TripleExponential`, `Adaptive`. |
| Indicator | `MomentumLength` | Raw momentum averaging depth (bars between the two price values). |
| Indicator | `FirstSmoothLength`, `SecondSmoothLength`, `ThirdSmoothLength` | Lengths of the three smoothing stages. |
| Indicator | `Phase` | Jurik phase parameter (used when smoothing method is `Jurik`). |
| Indicator | `PriceForClose`, `PriceForOpen` | Applied prices used for momentum (see code comments for formulas). |
| Logic | `SignalBar` | Bar index used for signals (0 = current closed bar, 1 = previous bar, etc.). |

## Usage notes
* Attach the strategy to a security and configure the candle series. The trading timeframe is the same as the indicator timeframe.
* The high-level API protection module is enabled automatically when stop/take profit values are positive.
* Margin modes are approximations because StockSharp does not expose MetaTrader-style balance/free margin. Risk-based modes rely on `StopLossPoints` and `Security.StepPrice`.
* Advanced smoothing methods from the original library (Parabolic, VIDYA, JurX) are mapped to the closest available StockSharp indicators (`TripleExponential` ≈ T3, `Adaptive` ≈ KAMA).
* Slippage parameter is preserved for completeness but market orders are used, so the value is informational.

## Getting started
1. Configure connection, portfolio, and security in your StockSharp environment.
2. Create an instance of `BlauCMomentumStrategy`, assign `Security`, `Portfolio`, and desired parameters.
3. Call `Start()`; the strategy will subscribe to candles, calculate the indicator, and trade automatically.
4. Monitor logs for information about opened/closed positions and indicator states.

## Risk disclaimer
This strategy is provided for educational purposes. Always validate performance with historical and forward tests before running it on a live account. Adjust risk settings to match your capital and market conditions.
