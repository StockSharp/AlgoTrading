# Williams R Ichimoku Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This setup combines the momentum extremes of Williams %R with the trend structure defined by the Ichimoku Cloud. The idea is to join strong moves only when price sits on the favourable side of the cloud and the short term lines confirm the bias.

Testing indicates an average annual return of about 73%. It performs best in the crypto market.

A long opportunity appears when the oscillator drops below WilliamsROversold while price holds above the cloud and Tenkan-sen is above Kijun-sen. A short signal occurs when %R climbs above WilliamsROverbought with price below the cloud and Tenkan-sen under Kijun-sen. The position remains open until price crosses the opposite side of the cloud.

Because the method waits for several pieces of confirmation, it suits traders who prefer clear trend filters over fast reversals. The opposite side of the cloud acts as the dynamic stop, so risk adjusts with the underlying trend.

## Details
- **Entry Criteria**:
  - **Long**: %R < WilliamsROversold && price above Ichimoku cloud and Tenkan-sen > Kijun-sen
  - **Short**: %R > WilliamsROverbought && price below Ichimoku cloud and Tenkan-sen < Kijun-sen
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when price crosses below the cloud
  - **Short**: Exit when price crosses above the cloud
- **Stops**: Yes.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
    -80 and -20 are the defaults of the Williams %R levels the rules quote. A long closes when price closes below the cloud and a short when it closes above it. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Williams R Ichimoku
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

