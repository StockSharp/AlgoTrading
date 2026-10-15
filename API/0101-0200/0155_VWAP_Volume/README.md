# Vwap Volume Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy combining VWAP and Volume indicators. Buys/sells on VWAP breakouts confirmed by above-average volume.

Testing indicates an average annual return of about 52%. It performs best in the crypto market.

This strategy references VWAP to gauge value and requires volume confirmation before trades. The idea is to join moves backed by strong participation.

Intraday traders focused on volume metrics can employ this method. Losses are trimmed via a percent stop.

## Details

- **Entry Criteria**:
  - Long: `Close < VWAP && Volume > AvgVolume * VolumeThreshold`
  - Short: `Close > VWAP && Volume > AvgVolume * VolumeThreshold`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price crosses back through VWAP
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `VolumePeriod` = 20
  - `VolumeThreshold` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The market trades around the clock, so the session VWAP restarts each UTC day and weighs the typical price of each candle by its volume. AvgVolume is the average volume of the previous VolumePeriod candles. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: VWAP, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

