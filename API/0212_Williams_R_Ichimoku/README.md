# Williams R Ichimoku Strategy

This setup combines the momentum extremes of Williams %R with the trend structure defined by the Ichimoku Cloud. The idea is to join strong moves only when price sits on the favourable side of the cloud and the short term lines confirm the bias.

A long opportunity appears when the oscillator drops below -80 while price holds above the cloud and Tenkan-sen crosses above Kijun-sen. A short signal occurs when %R climbs above -20 with price below the cloud and Tenkan-sen under Kijun-sen. The position remains open until price crosses the opposite side of the cloud.

Because the method waits for several pieces of confirmation, it suits traders who prefer clear trend filters over fast reversals. Dynamic stops are set around the Kijun-sen so risk adjusts with the underlying trend strength.

## Details
- **Entry Criteria**:
  - **Long**: %R < -80 && price above Ichimoku cloud and Tenkan-sen > Kijun-sen
  - **Short**: %R > -20 && price below Ichimoku cloud and Tenkan-sen < Kijun-sen
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when price crosses below the cloud
  - **Short**: Exit when price crosses above the cloud
- **Stops**: Yes.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
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
