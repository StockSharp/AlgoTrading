# Big Mover Catcher Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when the price closes above the upper Bollinger Band and all enabled filters confirm the move. It can also go short when the price closes below the lower band. Filters include RSI, ADX, ATR, EMA trend direction and MACD. A fixed percent stop loss is applied, positions are closed when price returns to the middle band, and an optional force take profit exits on unusually large candles.

## Details
- **Entry Criteria:**
  - **Long:** close > upper Bollinger Band and all active filters pass.
  - **Short:** close < lower Bollinger Band and all active filters pass.
- **Long/Short:** Both (configurable).
- **Exit Criteria:**
  - Price crosses the middle Bollinger Band.
  - Optional force take profit on large candles.
- **Stops:** Fixed percent stop loss.
- **Default Values:** Bollinger length = 40, stop loss = 2%, force TP threshold = 5%.
- **Filters:** RSI (14), ADX (28), ATR (14), EMA (350), MACD (12,26,9).
