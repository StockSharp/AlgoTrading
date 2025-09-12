# Price Convergence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy estimates the probability of price rising or falling by comparing the sum of OHLC4 values for bullish and bearish candles. A long position is opened when the probability of rising exceeds 50%, and a short position when the probability of falling exceeds 50%.

Testing indicates an average annual return of about 37%. It performs best in the crypto market.

The strategy can operate on the entire history or on a rolling window defined by the `Range` parameter. The OHLC4 value of each candle is used to weight contributions from up and down moves.

## Details

- **Entry Criteria**: Probability of rising above 50% triggers a long entry, probability of falling above 50% triggers a short entry.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `FullHistory` = true
  - `Range` = 200
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Statistical
  - Direction: Both
  - Indicators: Custom
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

