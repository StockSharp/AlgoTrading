# Genie RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades overbought and oversold reversals using the Relative Strength Index (RSI). When RSI rises above 80 the strategy opens a short position; when RSI falls below 20 it opens a long position. Optional take profit and trailing stop levels manage risk after entry.

The strategy is designed for oscillating markets where price frequently moves between support and resistance. It works on any timeframe, as defined by the `CandleType` parameter.

## Details

- **Entry Criteria**  
  - **Long**: RSI value crosses below 20 on a finished candle and no position is open.  
  - **Short**: RSI value crosses above 80 on a finished candle and no position is open.
- **Exit Criteria**  
  - **Long**: RSI rises above 80, price reaches the take profit distance, or price touches the trailing stop level.  
  - **Short**: RSI falls below 20, price reaches the take profit distance, or price touches the trailing stop level.
- **Indicators**: RSI.
- **Parameters**:  
  - `RSI Period` – length of the RSI indicator.  
  - `Take Profit` – distance in price units for profit target.  
  - `Trailing Stop` – distance in price units for trailing stop.  
  - `Candle Type` – timeframe of processed candles.
- **Position Management**: Uses market orders for entries and exits. Trailing stop is recalculated on each finished candle.
