# Vela Superada Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Vela Superada strategy trades a two-candle reversal pattern. A bullish setup occurs when a bearish candle is immediately followed by a bullish candle that closes above the prior open. Trades are filtered with a short-term EMA, RSI and MACD trend to avoid counter-trend signals. Both long and short sides can be enabled.

The strategy employs percentage-based take profit and stop loss levels and dynamically tightens a trailing stop once price moves favorably. This allows capturing extended moves while protecting against reversals.

## Details

- **Entry Criteria**:
  - **Long**: Previous candle bearish, current bullish, close and previous close above EMA, RSI < 65, MACD rising.
  - **Short**: Previous candle bullish, current bearish, close and previous close below EMA, RSI > 35, MACD falling.
- **Long/Short**: Configurable (long by default).
- **Exit Criteria**:
  - Trailing stop or opposite signal.
- **Stops**: Percent-based stop loss and take profit.
- **Default Values**:
  - `EmaLength` = 10
  - `RsiLength` = 14
  - `ShowLong` = True
  - `ShowShort` = False
  - `TpPercent` = 1.2
  - `SlPercent` = 1.8
- **Filters**:
  - Category: Pattern + indicators
  - Direction: Both
  - Indicators: EMA, RSI, MACD
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
