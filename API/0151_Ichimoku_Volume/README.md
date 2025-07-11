# Ichimoku Volume Strategy

Implementation of strategy #151 - Ichimoku + Volume. Buy when price is
above Kumo cloud, Tenkan-sen is above Kijun-sen, and volume is above
average. Sell when price is below Kumo cloud, Tenkan-sen is below Kijun-
sen, and volume is above average.

Ichimoku components define the directional bias while surging volume confirms interest. Trades open when price aligns with the cloud and volume picks up.

It fits traders who like to follow cloud breakouts with participation. Risk is restricted by an ATR-based stop.

## Details

- **Entry Criteria**:
  - Long: `Price > Cloud && Tenkan > Kijun && Volume > AvgVolume`
  - Short: `Price < Cloud && Tenkan < Kijun && Volume > AvgVolume`
- **Long/Short**: Both
- **Exit Criteria**:
  - Cloud breakout in opposite direction
- **Stops**: Percent-based using `StopLoss`
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Ichimoku Cloud, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
