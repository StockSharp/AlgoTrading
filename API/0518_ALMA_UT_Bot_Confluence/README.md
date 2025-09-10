# ALMA & UT Bot Confluence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The ALMA & UT Bot Confluence strategy combines an Arnaud Legoux Moving Average filter with a UT Bot style trailing stop. A long position opens when price is above both the long-term EMA and ALMA, volume exceeds its average, RSI signals momentum, ADX confirms trend strength, the candle is below the upper Bollinger Band and the UT Bot generates a buy signal. Short entries occur when the UT Bot turns bearish and price crosses below the fast EMA under the same filters. Exits use either the UT Bot trailing stop or fixed ATR-based stop loss and take profit.

## Details

- **Entry Criteria**:
  - Long: price > EMA & ALMA, RSI > 30, ADX > 30, price < Bollinger upper band, UT Bot buy signal, volume and ATR filters, cooldown.
  - Short: price crosses below fast EMA with UT Bot sell signal and filters.
- **Long/Short**: Both.
- **Exit Criteria**:
  - UT Bot trailing stop or ATR-based stop loss/take profit and optional time exit.
- **Stops**: ATR or UT Bot trailing.
- **Default Values**:
  - `FastEmaLength` = 20
  - `EmaLength` = 72
  - `AtrLength` = 14
  - `AdxLength` = 10
  - `RsiLength` = 14
  - `BbMultiplier` = 3.0
  - `StopLossAtrMultiplier` = 5.0
  - `TakeProfitAtrMultiplier` = 4.0
  - `UtAtrPeriod` = 10
  - `UtKeyValue` = 1
  - `VolumeMaLength` = 20
  - `BaseCooldownBars` = 7
  - `MinAtr` = 0.005
- **Filters**:
  - Category: Trend following with volatility filter
  - Direction: Long/Short
  - Indicators: EMA, ALMA, ADX, RSI, Bollinger Bands, UT Bot
  - Stops: ATR or trailing
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
