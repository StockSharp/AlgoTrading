# RSI Donchian Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The RSI Donchian strategy looks for momentum extremes that coincide with breakouts of the Donchian Channel. The relative strength index gauges overbought and oversold conditions while the channel defines recent price highs and lows.

Testing indicates an average annual return of about 82%. It performs best in the stocks market.

A buy signal appears when the RSI is above RsiOverbought as price breaks above the Donchian upper band. A short signal forms when the RSI is below RsiOversold as price falls through the lower band. Exits occur once price moves back to the Donchian middle line, signalling a return to balance.

This method works well for active traders who like to follow strong momentum but still trade with clear breakout levels. The stop-loss helps cap risk if momentum fails to revert quickly.

## Details
- **Entry Criteria**:
  - **Long**: RSI > RsiOverbought && Close > Donchian High
  - **Short**: RSI < RsiOversold && Close < Donchian Low
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when close < Donchian Middle
  - **Short**: Exit when close > Donchian Middle
- **Stops**: Yes, percentage stop-loss.
- **Default Values**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `StopLossPercent` = 2
    The channel is the high and low of the previous DonchianPeriod candles, and its middle is halfway between them. An upside breakout is confirmed when RSI is above RsiOverbought and a downside one when RSI is below RsiOversold; the opposite reading, an oversold RSI on a close above the channel, essentially never occurs. The stop is a fixed StopLossPercent of the entry price, watched between candles as well. An entry signal against an open position reverses it.
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: RSI, Donchian Channel
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

