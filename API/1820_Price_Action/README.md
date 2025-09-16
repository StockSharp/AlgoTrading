# Price Action Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Price Action Strategy** alternates between long and short market orders whenever the previous position closes.
It applies a fixed stop-loss distance, a leverage-based take-profit target, and an optional trailing stop that follows the market with a configurable step.

## Details
- **Entry Criteria:** No open position. Direction toggles between buy and sell after each trade.
- **Long/Short:** Both.
- **Exit Criteria:** Price hits the trailing stop, initial stop, or take-profit level.
- **Stops:** Fixed stop distance with optional trailing (step defines minimum price move for update).
- **Default Values:** `Volume = 1`, `TP = 100`, `Leverage = 5`, `TrailingStop = 0`, `TrailingStep = 0`, `InitialDirection = Buy`, `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`.
