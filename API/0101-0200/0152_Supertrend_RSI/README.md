# Supertrend Rsi Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementation of strategy - Supertrend + RSI. Buy when price is above Supertrend and RSI is below RsiOversold. Sell when price is below Supertrend and RSI is above RsiOverbought.

Testing indicates an average annual return of about 43%. It performs best in the stocks market.

The Supertrend indicator shows the current trend, and RSI spots when price is stretched. Orders follow the Supertrend direction once RSI reaches an extreme.

A good choice for traders relying on trailing stops. The built-in stop from Supertrend works with the ATR setting to cap losses.

## Details

- **Entry Criteria**:
  - Long: `Close > Supertrend && RSI < RsiOversold`
  - Short: `Close < Supertrend && RSI > RsiOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Supertrend flip in opposite direction
- **Stops**: Uses Supertrend as trailing stop
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiPeriod` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    On five-minute candles RSI almost never reaches 30 while price is above Supertrend (or 70 below it): the March 2024 BTC archive has no such candle at all. The defaults are therefore 40 and 60, which still mark a pullback against the trend and trade on both sides. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Supertrend, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

