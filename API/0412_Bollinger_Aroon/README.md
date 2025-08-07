# Bollinger Aroon
[Русский](README_ru.md) | [中文](README_cn.md)

The Bollinger Aroon strategy searches for pullbacks inside a strong uptrend.  
When price stretches beneath the lower Bollinger Band but the Aroon Up value
remains elevated, the system assumes the trend is intact and looks for a
reversion toward the mean.  It only trades long, seeking to capture the snap
back after a temporary dip.

The setup triggers after a finished candle closes below the lower band while
*Aroon Up* exceeds the confirmation level.  The position remains open until the
Aroon reading drops under a stop threshold or price rallies to the upper band.
The band width adapts to volatility, allowing the strategy to trade quiet and
active markets alike.

Backtests on major crypto pairs show the approach excels during strong trends
with occasional shakeouts.  Because entries require both volatility expansion
and a persistent Aroon Up reading, false signals are reduced compared with a
plain Bollinger reversal.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close below lower band AND `Aroon Up` > confirmation level.
  - **Short**: not used.
- **Exit Criteria**:
  - Close touches upper band OR `Aroon Up` < stop level.
- **Stops**: Indicator based; no fixed stop by default.
- **Default Values**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `AroonLength` = 288
  - `AroonConfirmation` = 90
  - `AroonStop` = 70
- **Filters**:
  - Category: Mean reversion within trend
  - Direction: Long only
  - Indicators: Bollinger Bands, Aroon
  - Complexity: Moderate
  - Risk level: Medium
