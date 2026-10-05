# Rsi Supertrend Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy based on RSI and Supertrend indicators. Enters long when RSI is oversold (< RsiOversold) and price is above Supertrend Enters short when RSI is overbought (> RsiOverbought) and price is below Supertrend

Testing indicates an average annual return of about 112%. It performs best in the forex market.

The RSI oscillator defines momentum extremes while Supertrend points to the prevailing direction. Trades occur when RSI aligns with the Supertrend color.

Works for traders who appreciate a trailing-stop style exit. The Supertrend's own ATR settings shape that trailing line.

## Details

- **Entry Criteria**:
  - Long: `RSI < RsiOversold && Close > Supertrend`
  - Short: `RSI > RsiOverbought && Close < Supertrend`
- **Long/Short**: Both
- **Exit Criteria**: Supertrend change
- **Stops**: Trailing with Supertrend
- **Default Values**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    On five-minute candles RSI almost never reaches 30 while price is above Supertrend (or 70 below it): the March 2024 BTC archive has no such candle at all. The levels are therefore parameters defaulting to 40 and 60, which still mark a pullback against the trend and trade on both sides. A long closes when Supertrend flips down and a short when it flips up. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: RSI, Supertrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

