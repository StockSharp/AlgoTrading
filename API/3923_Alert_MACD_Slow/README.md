[Русский](README_ru.md) | [中文](README_cn.md)

The **Alert MACD Slow** strategy reproduces the MetaTrader 4 expert `Alert_MACD_Slow.mq4`. It watches the MACD main line and two exponential moving averages and raises textual alerts when the indicator stack signals a potential breakout. No orders are submitted — the conversion stays faithful to the original advisor, which only displayed pop-up messages.

## Core Idea

1. Subscribe to the selected candle series and feed a MACD(3, 20, 9) together with fast and slow EMAs (20 and 65 periods).
2. Cache MACD values for the previous four completed candles to evaluate the slope transitions used by the MQL code.
3. Store the highs and lows of the last two candles to emulate the `High[1]/High[2]` and `Low[1]/Low[2]` breakout filters.
4. When the fast EMA stays above (or below) the slow EMA and the candle close breaks the memorised highs (or lows) while MACD turns upward (or downward) under the zero line, log the respective alert message.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `MacdFastPeriod` | `3` | Fast EMA length inside the MACD calculation. |
| `MacdSlowPeriod` | `20` | Slow EMA length used by the MACD. |
| `MacdSignalPeriod` | `9` | Signal smoothing period of the MACD. |
| `QuickEmaPeriod` | `20` | Period of the trend-following fast EMA (`Ma_Quick`). |
| `SlowEmaPeriod` | `65` | Period of the slow EMA trend filter (`Ma_Slow`). |
| `CandleType` | `TimeFrame(30m)` | Candle source passed to the indicator chain; choose a timeframe that matches your chart. |

## Alert Logic Details

- **MACD slope memory**: The strategy shifts the previous MACD values internally instead of calling `GetValue`, satisfying the conversion guidelines while preserving the original comparisons (`Macd_1 > Macd_2`, etc.).
- **Breakout check**: Closing prices above prior highs or below prior lows are treated as a proxy for the bid/ask checks from MetaTrader, which used the live quote against historical candle extremes.
- **Trend filter**: The alert triggers only when the fast EMA is on the correct side of the slow EMA, matching the long/short filters in the MQL expert.
- **Logging**: Alerts are sent through `AddInfoLog`. They include the four cached MACD values and the breakout levels to ease debugging and back-testing.
- **No trading**: Because the source advisor never placed trades, the StockSharp conversion keeps the strategy flat and focuses solely on signalling.

## Typical Usage

1. Attach the strategy to a symbol, configure the candle type to the desired timeframe, and keep the default indicator periods or adjust them for experimentation.
2. Start the strategy and wait until the MACD and EMA indicators become formed (several candles are needed because MACD requires history).
3. Watch the journal: when a bullish setup appears you will see `SET UP LONG`, while bearish setups produce `SET UP SHORT_VALUE`. The suffix mirrors the original alert text.
4. Use the printed diagnostics to decide whether to act manually or to chain the strategy with custom automation.

## Classification

- **Category**: Alerts / Trend Breakout Confirmation
- **Trading Direction**: None (signal-only)
- **Execution Style**: Event-driven on finished candles
- **Data Requirements**: Candle series compatible with the chosen `CandleType`
- **Complexity**: Moderate (multiple indicator filters, but straightforward state handling)
- **Risk Management**: Not applicable (no positions opened)

This port keeps the alerting behaviour of the MQL expert while leveraging StockSharp subscriptions, indicator bindings, and logging utilities.
