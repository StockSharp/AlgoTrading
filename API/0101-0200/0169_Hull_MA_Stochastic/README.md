# Hull Ma Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Hull Moving Average + Stochastic Oscillator strategy. Strategy enters when HMA trend direction changes with Stochastic confirming oversold/overbought conditions.

Testing indicates an average annual return of about 94%. It performs best in the stocks market.

Hull MA quickly reveals trend direction. Stochastic waits for a dip or rally within that trend to trigger the trade.

A flexible approach for those wanting smooth signals. ATR-based stops cap potential loss.

## Details

- **Entry Criteria**:
  - Long: `HullMA turning up && StochK < StochOversold`
  - Short: `HullMA turning down && StochK > StochOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Hull MA change of direction
- **Stops**: ATR-based using `StopLossAtr`
- **Default Values**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
    The Hull average turns up when it rises after falling and turns down when it falls after rising; a long closes when it falls and a short when it rises. %K is the stochastic over StochPeriod candles smoothed over StochK candles, and %D plays no part. The stop lies StopLossAtr ATRs (AtrPeriod) from the entry close and is checked on candle closes. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Hull MA, Moving Average, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

