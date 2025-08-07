# Flawless Victory Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Flawless Victory is a modular momentum system that blends oscillators with Bollinger Bands. Depending on the selected version it can operate with simple RSI signals, apply fixed take-profit and stop-loss targets, or demand confirmation from the Money Flow Index. The goal is to exploit exhaustion at the edges of volatility bands and ride mean-reversion swings.

Version 1 enters when RSI leaves oversold or overbought zones near the Bollinger extremes. Version 2 adds explicit risk control via percentage based targets. Version 3 requires both RSI and MFI to agree, filtering out weak reversals.

The strategy performs best on intraday markets with clear volatility boundaries.

## Details

- **Entry Criteria**:
  - **Long**: see version rules (RSI <30 near lower band; version 3 also `MFI < 20`)
  - **Short**: RSI >70 near upper band (version 3 also `MFI > 80`)
- **Long/Short**: Both sides
- **Exit Criteria**:
  - **Version 1**: opposite RSI signal
  - **Version 2**: take-profit or stop-loss percentages
  - **Version 3**: opposite RSI/MFI combo
- **Stops**: Optional in version 2
- **Default Values**:
  - `RSI_length` = 14
  - `MFI_length` = 14
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `TakeProfitPct` = 1.5
  - `StopLossPct` = 1.0
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: RSI, MFI, Bollinger Bands
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
