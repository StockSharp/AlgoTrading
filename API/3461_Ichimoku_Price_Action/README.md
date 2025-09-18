# Ichimoku Price Action Strategy

## Overview
The **Ichimoku Price Action Strategy** is a time-filtered MACD momentum system ported from the MQL4 expert "Ichimoku Price Action Strategy v1.0" into the StockSharp high-level API. The original EA opened market orders whenever trading was enabled for the instrument and the optional MACD filter confirmed the direction. This C# port keeps the same idea while providing detailed risk controls for stop-loss placement, break-even handling and trailing exits.

The strategy is designed for discretionary traders who want to automate a time-of-day directional play with minimal indicator dependencies. All trading signals are evaluated on completed candles of the chosen trading timeframe, while supporting auxiliary timeframes for ATR- and swing-based protective stops.

> **Important:** The StockSharp version maintains at most one net position at a time. Hedge-style simultaneous long/short exposure from the original template is not supported because the StockSharp `Strategy` operates on net positions. All other money-management features are expressed through stop, target and trailing logic executed on every finished candle.

## Trading Logic
1. **Session filter** – Entries are allowed only when the current time-of-day is inside the `[StartTime; EndTime]` window. Setting both parameters to `00:00` disables the session filter.
2. **MACD confirmation (optional)** – When `UseMacdFilter = true`, longs require MACD main line above the signal line, shorts require the opposite. MACD settings are fully configurable.
3. **Order placement** – If trading is enabled for a direction and no position is open, the strategy sends a market order with the configured `Volume`.
4. **Protective stops** – Depending on `StopLossMode`, the initial stop is placed using a fixed pip distance, an ATR multiple or the latest swing extreme gathered from a lower timeframe. The stop is recalculated on every candle and tightened when the newly computed level is more conservative.
5. **Targets** – A fixed pip target or a dynamic risk/reward target based on the active stop is checked every candle. Once reached, the position is closed at market.
6. **Break-even and trailing** – When unrealised profit reaches `MoveToBreakEven`, the stop is pulled to the entry price. After `TrailingTrigger` pips of profit the trailing module activates and keeps pushing the stop every time price improves by `TrailingStep` pips while maintaining a distance of `TrailingStop` pips from the candle close.
7. **Reverse exit** – If `CloseOnReverse = true`, any opposite entry signal immediately closes the current position before potentially flipping in the new direction.

## Risk Management
- **Stop loss**
  - *Fixed pips* – Uses `StopLossPips` multiplied by the instrument price step.
  - *ATR multiplier* – Uses the latest ATR value from `AtrCandleType` multiplied by `AtrMultiplier`.
  - *Swing high/low* – Uses the most recent swing extreme calculated by `SwingCandleType` with `SwingBars` lookback.
- **Take profit**
  - *Fixed pips* – Uses `TakeProfitPips`.
  - *Risk/Reward* – Uses the current stop distance multiplied by `TakeProfitRatio`.
- **Break-even** – `MoveToBreakEven` defines how many profitable pips are required before the stop is locked at the entry price.
- **Trailing** – Controlled by `TrailingStop`, `TrailingTrigger` and `TrailingStep` to maintain profits once the market moves favourably.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| General | `BuyMode` | Allow long entries. |
| General | `SellMode` | Allow short entries. |
| General | `CandleType` | Trading timeframe (default 1 hour). |
| Schedule | `StartTime` / `EndTime` | Session window in exchange time (00:00 → disabled). |
| Filters | `UseMacdFilter` | Enable MACD confirmation. |
| Filters | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD periods for fast EMA, slow EMA and signal EMA. |
| Risk | `StopLossMode` | Stop-loss calculation: `FixedPips`, `AtrMultiplier`, `SwingHighLow`. |
| Risk | `StopLossPips` | Distance in pips when fixed mode is selected. |
| Risk | `AtrMultiplier`, `AtrPeriod`, `AtrCandleType` | ATR-based stop configuration. |
| Risk | `SwingBars`, `SwingCandleType` | Swing high/low stop configuration. |
| Risk | `TakeProfitMode` | Target mode: `FixedPips` or `RiskReward`. |
| Risk | `TakeProfitPips`, `TakeProfitRatio` | Target distances. |
| Risk | `CloseOnReverse` | Close the active position when the opposite signal appears. |
| Orders | `Volume` | Market order volume (lots/contracts). |
| Risk | `MoveToBreakEven` | Profit threshold (in pips) to move stop to entry. |
| Risk | `TrailingStop`, `TrailingTrigger`, `TrailingStep` | Trailing stop configuration in pips. |

## Usage Notes
- Ensure that the instrument has `PriceStep` defined; otherwise the strategy assumes a pip size of `0.0001`.
- When ATR or swing stops are enabled, the corresponding auxiliary subscriptions are automatically added. Make sure the data feed supplies those timeframes.
- If you need to disable break-even or trailing behaviour set the corresponding parameters to `0`.
- The strategy is neutral by default at the session open. It will not stack multiple positions in the same direction; re-entries happen only after the previous trade is closed.

## Limitations Compared to MQL Version
- Only net positions are supported (StockSharp limitation). Hedge-style simultaneous long and short trades are not reproduced.
- Money-management modes such as Kelly sizing or partial profit-taking are not part of this port.
- Manual confirmation, dashboard graphics and screenshot features of the MQL template are intentionally omitted.

## Backtesting Checklist
1. Configure the desired `CandleType` and auxiliary timeframes.
2. Adjust `Volume` and stop/target parameters to match the original EA settings.
3. Enable or disable MACD confirmation depending on the template usage.
4. Run simulation ensuring that the trading session window matches your original tests.
5. Review the generated log messages to confirm stop and target events happen as expected.
