# Hull Suite Strategy – 1% Risk, No SL/TP
[Русский](README_ru.md) | [中文](README_cn.md)

The Hull Suite strategy opens long positions when the selected Hull moving average rises compared to two bars ago and opens short positions when it falls. No stop loss or take profit is used.

## Details

- **Entry Criteria**:
  - **Long**: Hull value greater than the value two bars ago.
  - **Short**: Hull value less than the value two bars ago.
- **Long/Short**: Both sides.
- **Exit Criteria**: Reverse position on opposite signal.
- **Stops**: None.
- **Default Values**:
  - `HullLength` = 55
  - `Mode` = Hma
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: HMA, EHMA, THMA
  - Stops: None
  - Complexity: Low
  - Timeframe: 5m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low

