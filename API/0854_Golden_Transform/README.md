# Golden Transform Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Rate of Change indicator with a triple Hull-based TRIX, a Hull MA filter and a smoothed Fisher Transform. Long trades are opened when ROC crosses above TRIX while TRIX is below zero and the opening price is above the Hull MA. Short trades occur on the opposite signal. Positions are closed on opposite crosses or when the smoothed Fisher exceeds thresholds and reverses.

## Details

- **Entry Criteria**:
  - **Long**: `ROC crosses above TRIX` && `TRIX < 0` && `Open > Hull MA`
  - **Short**: `ROC crosses below TRIX` && `TRIX > 0` && `Open < Hull MA`
- **Long/Short**: Long and Short
- **Exit Criteria**:
  - Long: `ROC crosses below TRIX` OR (`Fisher HMA > 1.5` && `Fisher HMA crosses below previous Fisher`)
  - Short: `ROC crosses above TRIX` OR (`Fisher HMA < -1.5` && `Fisher HMA crosses above previous Fisher`)
- **Stops**: No
- **Default Values**:
  - `ROC Length` = 50
  - `Hull TRIX Length` = 90
  - `Hull Entry Length` = 65
  - `Fisher Length` = 50
  - `Fisher Smooth Length` = 5
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ROC, Hull MA, Fisher Transform
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
