# Average Candle Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy recreates the "Average candle cross" MetaTrader expert. It waits for a completed bar where the previous candle closed across a moving average while two additional moving-average filters already confirm the prevailing trend. Only one position can be active at a time. Immediately after opening a trade the algorithm attaches a stop-loss and a take-profit whose distance is derived from the specified pip-based stop. This makes the behaviour identical to the original block logic that fires once per bar.

The entry logic reads historical bar data instead of unfinished ticks, so all signals are evaluated at the close of the latest finished candle. Separate parameter sets control the bullish and bearish filters, allowing asymmetric smoothing or period lengths. The protective levels are created with native stop and limit orders positioned at `StopLossPips * PipSize` away from the entry price. The take-profit reuses the same stop distance and multiplies it by the percentage factor defined for each side.

## Details

- **Entry Criteria**:
  - **Long**: The fast and slow trend filters for the long side are both rising on the previous bar (`MA_fast1[1] > MA_slow1[1]` and `MA_fast2[1] > MA_slow2[1]`) and the previous candle closes above its dedicated average while the candle from two bars ago was below it (`Close[2] <= MA_cross[2]` and `Close[1] > MA_cross[1]`).
  - **Short**: The fast and slow trend filters for the short side are both declining on the previous bar (`MA_fast1[1] < MA_slow1[1]` and `MA_fast2[1] < MA_slow2[1]`) and the previous candle closes below its dedicated average while the candle from two bars ago was above it (`Close[2] >= MA_cross[2]` and `Close[1] < MA_cross[1]`).
- **Long/Short**: Both directions, but never simultaneously.
- **Exit Criteria**:
  - Positions are closed exclusively by the protective stop-loss or the take-profit orders.
- **Stops**: Yes. The stop is placed `StopLossPips * PipSize` away from the entry price; the take-profit equals the stop distance multiplied by the `% of SL` parameter.
- **Default Values**:
  - `FirstTrendFastPeriod` = 5, `FirstTrendFastMethod` = SMA.
  - `FirstTrendSlowPeriod` = 20, `FirstTrendSlowMethod` = SMA.
  - `SecondTrendFastPeriod` = 20, `SecondTrendFastMethod` = SMA.
  - `SecondTrendSlowPeriod` = 30, `SecondTrendSlowMethod` = SMA.
  - `BullCrossPeriod` = 5, `BullCrossMethod` = SMA.
  - `BuyVolume` = 0.01, `BuyStopLossPips` = 50, `BuyTakeProfitPercent` = 100.
  - `FirstTrendBearFastPeriod` = 5, `FirstTrendBearFastMethod` = SMA.
  - `FirstTrendBearSlowPeriod` = 20, `FirstTrendBearSlowMethod` = SMA.
  - `SecondTrendBearFastPeriod` = 20, `SecondTrendBearFastMethod` = SMA.
  - `SecondTrendBearSlowPeriod` = 30, `SecondTrendBearSlowMethod` = SMA.
  - `BearCrossPeriod` = 5, `BearCrossMethod` = SMA.
  - `SellVolume` = 0.01, `SellStopLossPips` = 50, `SellTakeProfitPercent` = 100.
  - `PipSize` = 0.0001.
- **Filters**:
  - Category: Trend following.
  - Direction: Dual (long + short).
  - Indicators: Multiple moving averages.
  - Stops: Fixed pip-based stop and proportional take-profit.
  - Complexity: Moderate.
  - Timeframe: Works on the configured candle series (default 15 minutes).
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
