# MFI Level Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Money Flow Index (MFI) oscillator to identify overbought and oversold conditions. When the MFI crosses predefined threshold levels, the strategy enters or reverses positions. It can operate either in the direction of the crossing or in the opposite direction, depending on the selected trend mode.

The default configuration monitors four-hour candles and evaluates the 14-period MFI. The strategy opens a long position when the MFI falls below the lower threshold and a short position when it rises above the upper threshold. When set to "Against" mode, the entry logic is reversed to trade against the indicator direction.

Risk management is handled through built-in stop-loss and take-profit parameters expressed as percentages from the entry price.

## Details

- **Entry Criteria**:
  - **Trend Mode: Direct**:
    - **Long**: Previous MFI > Low level and current MFI ≤ Low level.
    - **Short**: Previous MFI < High level and current MFI ≥ High level.
  - **Trend Mode: Against**:
    - **Long**: Previous MFI < High level and current MFI ≥ High level.
    - **Short**: Previous MFI > Low level and current MFI ≤ Low level.
- **Long/Short**: Both sides.
- **Exit Criteria**: Position is reversed when the opposite signal appears or closed by protection module.
- **Stops**: Stop-loss and take-profit expressed in percent from entry price.
- **Default Values**:
  - `Candle Type` = 4-hour candles.
  - `MFI Period` = 14.
  - `Low Level` = 40.
  - `High Level` = 60.
  - `Stop Loss %` = 1.
  - `Take Profit %` = 2.
- **Filters**:
  - Category: Oscillator
  - Direction: Configurable
  - Indicators: Money Flow Index
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Notes

This implementation relies on StockSharp's high-level API. It subscribes to candle data, binds the MFI indicator directly, and executes market orders when crossing conditions are met. Position protection is initialized once at startup to manage risk automatically.
