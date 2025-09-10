# Bober XM Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Bober XM strategy uses a dual-channel approach based on a custom Keltner calculation. Breakout entries are confirmed by a Weighted Moving Average and overall trend strength from ADX. Exits rely on On-Balance Volume crossing its moving average while ADX remains strong.

Designed for traders seeking momentum confirmation with volume-based exits.

## Details

- **Entry Criteria**:
  - **Long**: `Close > UpperBand && Close > WMA && ADX > Threshold`
  - **Short**: `Close < LowerBand && Close < WMA && ADX > Threshold`
- **Long/Short**: Both
- **Exit Criteria**:
  - **Long**: `OBV < OBV_MA && ADX > Threshold`
  - **Short**: `OBV > OBV_MA && ADX > Threshold`
- **Stops**: percent stop-loss via `StopLossPercent`
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `KeltnerMultiplier` = 1.5m
  - `WmaPeriod` = 15
  - `ObvMaPeriod` = 22
  - `AdxPeriod` = 60
  - `AdxThreshold` = 35m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Keltner Channel, WMA, OBV, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

