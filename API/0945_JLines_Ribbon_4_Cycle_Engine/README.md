# J-Lines Ribbon 4-Cycle Engine Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The J-Lines Ribbon 4-Cycle Engine strategy classifies the market into CHOP, LONG and SHORT cycles using a ribbon of EMAs and the Average Directional Index. Entries occur on new cycle detections and rebounds from key EMAs, while exits trigger on opposite crossings or swing breaks.

## Details

- **Entry Criteria**:
  - **Long**: New LONG cycle or rebound above EMA72/EMA126 while EMA72 is above EMA89.
  - **Short**: New SHORT cycle or rebound below EMA72/EMA126 while EMA72 is below EMA89.
- **Stops**: Last swing high/low.
- **Default Values**:
  - `DmiLength` = 8
  - `AdxFloor` = 12
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, ADX
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
