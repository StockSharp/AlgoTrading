# Super Trend
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Supertrend indicator

Super Trend calculates a dynamic line from ATR that flips between support and resistance. Price crossing above it turns the bias bullish, and crossing below turns it bearish. The trade ends when the line reverses.

By following this adaptive line, the strategy attempts to capture sustained runs while minimizing whipsaws. Because the stop level trails price, it locks in profits once momentum fades.


## Details

- **Entry Criteria**: Signals based on ATR, Supertrend.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, Supertrend
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 67%. It performs best in the stocks market.
