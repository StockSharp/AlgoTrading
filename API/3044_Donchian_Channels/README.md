# Donchian Channels Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the classic "Donchian Channels" expert advisor to the StockSharp high level API. It combines a multi-timeframe Donchian breakout with weighted moving averages, momentum confirmation, MACD trend filtering, and extensive risk controls (stop loss, take profit, break-even, trailing stop and equity-based emergency exit).

## Logic Overview

- **Market Regime:**
  - Donchian Channel is calculated on a higher timeframe (default 4-hour) to detect the prevailing breakout structure.
  - A MACD computed on a configurable trend timeframe (daily by default) ensures the higher timeframe trend agrees with the trade direction.
- **Entry Conditions:**
  - **Long setup:**
    - Lower Donchian band or channel median penetrates the previous higher-timeframe candle body from below, signalling a potential breakout.
    - Previous two base-timeframe candles form an upward swing (`Low[2] < High[1]`).
    - Momentum absolute deviation from 100 on the higher timeframe exceeds the buy threshold in any of the last three readings.
    - Fast LWMA stays within the configured distance above the slow LWMA to avoid overstretched moves.
    - MACD main line is above its signal (both positive or both negative) confirming bullish bias.
  - **Short setup:** Symmetric rules mirrored for the upper Donchian band, swing structure, bearish momentum deviation and MACD confirmation.
  - Multiple entries (pyramiding) are allowed until the configured maximum trade count is reached.
- **Exit Conditions:**
  - Fixed stop loss and take profit defined in price steps.
  - Optional move-to-break-even once price progresses a configurable distance beyond entry.
  - Trailing stop that can either follow recent candle extremes (with padding) or trail price using a classic trigger/step approach.
  - Equity stop monitors strategy P&L drawdown and forces flat when losses breach the allowed risk budget.

## Parameters

| Group | Name | Description |
| ----- | ---- | ----------- |
| General | Base Candle | Execution timeframe for entries and risk checks. |
| General | Donchian Candle | Higher timeframe for Donchian channel and momentum filter. |
| General | Trend Candle | Timeframe used by the MACD trend filter. |
| General | Volume | Order size for each entry. |
| Indicators | Donchian Length | Lookback period for Donchian Channel. |
| Indicators | Fast MA / Slow MA | Lengths of the weighted moving averages on the trading timeframe. |
| Indicators | MA Distance | Maximum allowed distance between fast and slow LWMA (in price steps). |
| Indicators | Momentum Period | Lookback for the momentum filter on the higher timeframe. |
| Filters | Momentum Buy / Sell | Minimum absolute deviation from 100 required for bullish/bearish momentum. |
| Risk | Stop Loss / Take Profit | Hard exits measured in price steps from the entry price. |
| Risk | Use Trailing | Enables trailing stop management. |
| Risk | Trailing Trigger / Step | Classic trailing parameters when candle-based trailing is disabled. |
| Risk | Candle Trail / Trail Candles | Toggles candle-based trailing and sets the number of candles used. |
| Risk | Trailing Padding | Extra buffer applied around candle extremes. |
| Risk | Use BreakEven | Enables move-to-break-even. |
| Risk | BreakEven Trigger / Offset | Distance and offset applied when moving the stop to break-even. |
| Risk | Use Equity Stop | Activates drawdown-based emergency exit. |
| Risk | Equity Risk | Maximum allowed drawdown before flattening the position. |
| Risk | Max Trades | Maximum number of concurrent pyramid entries. |

## Usage Tips

1. **Timeframes:** Align the base timeframe with your execution style (e.g., 1h/4h) and keep the Donchian/MACD timeframes higher to maintain the multi-timeframe confirmation logic.
2. **Momentum Thresholds:** The original EA measured momentum deviations around 100. Start with small thresholds (0.3) and increase to filter out weaker moves in choppy markets.
3. **Risk Settings:** Convert pip-based distances from the MQL version into instrument-specific price steps. Always verify the security `Step` value when configuring stops and trailing logic.
4. **Pyramiding:** Reduce `Max Trades` to 1 if you prefer single-position management. Increase it gradually when testing pyramid behaviour.
5. **Equity Stop:** The equity stop monitors strategy P&L inside StockSharp. Adjust `Equity Risk` to reflect the maximum drawdown (in account currency) you are willing to tolerate.

## Backtesting

- Works directly inside StockSharp Designer/Backtester using candle subscriptions only (no tick-level data required).
- Ensure that all selected timeframes are available from the data provider before launching a backtest or live session.
- When optimizing, prioritise Donchian length, MA distance and momentum thresholds—they have the strongest impact on win rate and trade frequency.
