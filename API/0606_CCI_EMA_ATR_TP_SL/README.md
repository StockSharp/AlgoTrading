# CCI + EMA Strategy with Percentage or ATR TP/SL
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Commodity Channel Index (CCI) with an optional EMA trend filter and RSI confirmation.
Positions are opened when CCI exits extreme zones and optional filters allow trading.
Take profit and stop loss can be calculated either as percentages of the entry price or using ATR-based levels with a risk-reward ratio.

## Details

- **Entry Conditions:**
  - **Long:** CCI crosses above the oversold level, price above EMA (if enabled), RSI below oversold (if enabled).
  - **Short:** CCI crosses below the overbought level, price below EMA (if enabled), RSI above overbought (if enabled).
- **Exit Conditions:**
  - Take-profit or stop-loss levels reached.
  - Long positions close when CCI crosses above the overbought level.
  - Short positions close when CCI crosses below the oversold level.

Default parameters follow the original script.
