# Gold Scalping BOS & CHoCH Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades break of structure (BOS) and change of character (CHoCH) patterns on gold. It derives short-term support and resistance levels and enters when a BOS is followed by an immediate CHoCH, using dynamic stop loss and take profit targets.

## Details

- **Entry Criteria**:
  - **Long**: `High > LastSwingHigh` and `Close` crosses above `LastSwingLow`
  - **Short**: `Low < LastSwingLow` and `Close` crosses below `LastSwingHigh`
- **Long/Short**: Both sides
- **Exit Criteria**: Stop loss or take profit
- **Stops**: Dynamic
- **Default Values**:
  - `RecentLength` = 10
  - `SwingLength` = 5
  - `TakeProfitFactor` = 2
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
