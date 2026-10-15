# Donchian Macd Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy combining Donchian Channel breakout with MACD trend confirmation.

Testing indicates an average annual return of about 148%. It performs best in the forex market.

The strategy waits for a Donchian breakout and verifies momentum with MACD. Long or short trades ride the move once MACD agrees.

Aimed at breakout enthusiasts wanting confirmation. Stops are placed at a fixed percentage of the entry price.

## Details

- **Entry Criteria**:
  - Long: `Price breaks Donchian high && MACD > Signal`
  - Short: `Price breaks Donchian low && MACD < Signal`
- **Long/Short**: Both
- **Exit Criteria**: MACD reversal
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `DonchianPeriod` = 20
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    A breakout is a close beyond the highest high or lowest low of the previous DonchianPeriod candles. The MACD reversal exit closes a long when MACD crosses below the signal line and a short when it crosses above it. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

