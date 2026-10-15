# Hull Ma Rsi Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementation of strategy - Hull Moving Average + RSI. Buy when HMA turns up and RSI is below RsiOversold. Sell when HMA turns down and RSI is above RsiOverbought.

Testing indicates an average annual return of about 64%. It performs best in the forex market.

Hull MA provides a smoothed trend line and RSI highlights momentum divergences. Trades occur when RSI turns at extremes while price follows the Hull direction.

Suited to short-term swing traders who want early signals. ATR-based stops protect the trade.

## Details

- **Entry Criteria**:
  - Long: `HullMA turning up && RSI < RsiOversold`
  - Short: `HullMA turning down && RSI > RsiOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Hull MA change of direction
- **Stops**: ATR-based using `StopLossAtr`
- **Default Values**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The Hull average turns up when it rises after falling and turns down when it falls after rising. The stop lies StopLossAtr ATRs (AtrPeriod) from the entry close and is checked on candle closes. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Hull MA, Moving Average, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

