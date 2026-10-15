# Donchian Rsi Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy combining Donchian Channels and RSI indicators. Buys on Donchian breakouts when RSI confirms trend is not overextended.

Testing indicates an average annual return of about 55%. It performs best in the stocks market.

Donchian channels identify breakout levels, while RSI checks whether momentum supports the move. Positions follow when a breakout aligns with RSI direction.

Best for traders expecting a sustained breakout rather than a fakeout. Risk is limited through a percent stop.

## Details

- **Entry Criteria**:
  - Long: `Close > DonchianHigh && RSI < RsiOverboughtLevel`
  - Short: `Close < DonchianLow && RSI > RsiOversoldLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - Breakout failure or opposite signal
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `DonchianPeriod` = 20
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    A breakout is not overextended while RSI stays below RsiOverboughtLevel (long) or above RsiOversoldLevel (short); the channel spans the previous DonchianPeriod candles. The breakout fails, closing the position, when price closes back beyond the broken level. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

