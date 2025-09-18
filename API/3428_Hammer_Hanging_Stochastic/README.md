# Hammer Hanging Stochastic
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader expert "Expert_AH_HM_Stoch" to the StockSharp high-level API. It combines hammer and hanging man candlestick patterns with stochastic oscillator confirmation to capture reversal setups after extended moves.

The strategy waits for a completed candle before acting, uses the stochastic signal line for filtering, and closes positions when momentum exits the extreme zones.

## Details

- **Entry Criteria**:
  - Long: Bullish hammer candle and stochastic %D (previous bar) below the oversold level.
  - Short: Bearish hanging man candle and stochastic %D (previous bar) above the overbought level.
- **Long/Short**: Both.
- **Exit Criteria**: Close positions when stochastic %D crosses above/below configurable recovery and extreme levels.
- **Stops**: Enabled through the built-in `StartProtection()` hook (defaults to account-level protection).
- **Default Values**:
  - `CandleType` = TimeSpan.FromHours(1)
  - `StochPeriodK` = 15
  - `StochPeriodD` = 49
  - `StochPeriodSlow` = 25
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `ExitLowerLevel` = 20
  - `ExitUpperLevel` = 80
  - `MaxBodyRatio` = 0.35
  - `LowerShadowMultiplier` = 2.5
  - `UpperShadowMultiplier` = 0.3
- **Filters**:
  - Category: Pattern + Oscillator confirmation
  - Direction: Both
  - Indicators: Candlestick, Stochastic
  - Stops: Optional risk controls via `StartProtection`
  - Complexity: Intermediate
  - Timeframe: Swing / Intraday (1h default)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Moderate

## How It Works

1. Subscribes to the configured candle series and stochastic oscillator using the high-level `BindEx` API.
2. Detects hammer and hanging man formations based on body and shadow ratios.
3. Confirms entries with the stochastic %D line using the previous closed bar value.
4. Manages exits when the stochastic exits the oversold/overbought zones, mirroring the logic of the original MQL expert.
5. Provides chart visualization for candles, stochastic, and own trades when a chart area is available.
