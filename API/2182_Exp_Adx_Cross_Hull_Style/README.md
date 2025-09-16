# Exp ADX Cross Hull Style Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines Average Directional Index (ADX) cross signals with a Hull Moving Average (HMA) filter. Entries occur when the +DI line crosses above the -DI line for longs or below for shorts. Exits are handled by a pair of Hull moving averages: the fast average crossing the slow average closes positions. The strategy operates on the 4-hour timeframe by default.

## Details
- **Entry Criteria**  
  - **Long**: +DI crosses above -DI.  
  - **Short**: -DI crosses above +DI.
- **Exit Criteria**  
  - **Long**: fast HMA falls below slow HMA.  
  - **Short**: fast HMA rises above slow HMA.
- **Indicators**  
  - AverageDirectionalIndex (period 14).  
  - HullMovingAverage fast length 20.  
  - HullMovingAverage slow length 50.
- **Timeframe**: 4-hour candles (configurable).
- **Stops**: none by default.
- **Direction**: both long and short.

The strategy does not rely on historical collections; it reacts to streaming candle data. Parameters can be optimized for different markets. Chart output displays price candles with both Hull moving averages and trade marks.
