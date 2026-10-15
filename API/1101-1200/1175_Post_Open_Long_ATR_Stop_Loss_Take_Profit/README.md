# Post Open Long ATR Stop Loss Take Profit Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The strategy enters a long position during market open after a breakout from resistance while price stays near the Bollinger basis. It uses EMA, RSI, ADX and ATR filters and exits via ATR-based stop loss and take profit.

## Details

- **Entry Criteria**:
  - **Long**: Breakout above recent resistance within market open, price near Bollinger middle band, RSI above threshold, ADX above threshold, short-term trend up and no pullback.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - ATR stop loss or take profit reached.
- **Stops**:
  - ATR-based stop loss and take profit.
- **Default Values**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
  - `SessionStart` = 09:30 UTC
  - `SessionDuration` = TimeSpan.FromHours(1)
  - `ResistancePeriod` = 5
  - `BasisProximity` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Calculation and execution

Signals use finished candles after all indicators are formed. Resistance is the maximum high of the previous `ResistancePeriod` candles, excluding the current candle; entry requires `Close > resistance`. The opening window uses UTC candle-open time: start inclusive, end exclusive, wrapping across midnight when necessary. Configure `SessionStart` for the instrument's opening time.

Bollinger basis is the current-inclusive close SMA; deviation is the population standard deviation. Proximity requires `abs(Close - basis) <= (UpperBand - basis) * BasisProximity`. Both `short EMA > long EMA` and `Close > short EMA` are required. No pullback means `Close >= Open` on the signal candle. RSI and ADX must be strictly above their thresholds.

Buy `Volume` only from a flat position with no outstanding order. Entry Wilder ATR freezes the stop and take distances. Native local protection places them below/above the actual fill price (volume-weighted for partial fills), watches Level1 best-bid and candle prices, and closes the entire remaining long. Exits remain active outside the opening window. Later ATR changes do not move these levels. There are no shorts, EMA-cross exits or cooldown.
