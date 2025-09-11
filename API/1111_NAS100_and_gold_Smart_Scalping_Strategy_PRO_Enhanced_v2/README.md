# NAS100 and Gold Smart Scalping Strategy PRO Enhanced v2
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy scalps short-term moves using EMA9 and VWAP as dynamic guides, RSI for momentum, and ATR for risk management. A 15 minute EMA200 trend filter keeps trades with the prevailing trend while a volume spike filter seeks strong candles. Positions size by risk and support optional trailing stops and cooldown periods between trades.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss, take-profit or opposite signal
- **Stops**: Yes, ATR based
- **Default Values**:
  - `CandleType` = 1 minute
  - `RiskPercent` = 1%
  - `AtrMultiplierSl` = 1
  - `AtrMultiplierTp` = 2
  - `CooldownMins` = 30
  - `StartHour` = 13
  - `EndHour` = 20
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: EMA, VWAP, RSI, ATR, EMA200
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
