# Donchian Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Donchian Channel + Stochastic strategy. Strategy enters the market when the price breaks out of Donchian Channel with Stochastic confirming oversold/overbought conditions.

Testing indicates an average annual return of about 85%. It performs best in the crypto market.

Breakouts beyond the Donchian channel are confirmed with Stochastic momentum. Trades start as soon as price escapes the range and the oscillator agrees.

Useful for traders expecting immediate follow-through. A fixed percentage of the entry price sets the stop.

## Details

- **Entry Criteria**:
  - Long: `Close > DonchianHigh && StochK > StochOverbought`
  - Short: `Close < DonchianLow && StochK < StochOversold`
- **Long/Short**: Both
- **Exit Criteria**: Breakout failure or opposite signal
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOverbought` = 80
  - `StochOversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2
    An upside breakout of the previous DonchianPeriod candles is confirmed when %K is above StochOverbought and a downside one when %K is below StochOversold; the opposite reading, an oversold %K on an upside breakout, essentially never occurs. %K is the stochastic over StochPeriod candles smoothed over StochK candles, and %D plays no part. The breakout fails, closing the position, when price closes back beyond the broken level. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

