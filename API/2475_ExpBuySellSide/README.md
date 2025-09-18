# ExpBuySellSide Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the MetaTrader Expert Advisor **ExpBuySellSide** to the StockSharp API. It merges an ATR-based stop system with a simplified Step Up/Down trend filter.

The ATR module calculates dynamic stop levels around each candle. When price breaks above the upper band the market is considered to be in a bullish phase; breaking below the lower band indicates a bearish phase.

The Step Up/Down module compares a very fast SMA with a slower SMA and checks whether the spread between them is expanding. Growing spread in the direction of the crossover confirms the trend.

A trade is opened only when **both** modules point in the same direction. Existing positions may optionally be closed when an opposite signal appears.

## Details

- **Entry Criteria**:
  - **Long**: price closes above the ATR upper band **and** the fast SMA rises away from the slow SMA.
  - **Short**: price closes below the ATR lower band **and** the fast SMA falls away from the slow SMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal appears and the option *Close Opposite* is enabled.
  - Manual stop using position protection.
- **Stops**: Based on `ATR * Multiplier` bands.
- **Default Values**:
  - `ATR Period` = 5.
  - `ATR Multiplier` = 2.5.
  - `Fast SMA` = 2.
  - `Slow SMA` = 30.
  - `Candle Type` = 1 hour time frame.
  - `Close Opposite` = true.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

