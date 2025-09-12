# Trend Trader-Remastered Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy uses the Parabolic SAR indicator to follow trends. A buy order is sent when price crosses above the SAR and a sell order when price crosses below. An opposite cross closes the current position.

## Details

- **Entry Criteria**:
  - **Long**: Price crosses above PSAR.
  - **Short**: Price crosses below PSAR.
- **Exits**: Opposite PSAR cross closes the trade.
- **Stops**: No additional stops.
- **Default Values**:
  - `Start` = 0.02
  - `Increment` = 0.02
  - `Max` = 0.2
