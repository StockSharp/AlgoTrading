# Adaptive Squeeze Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Adaptive Squeeze Momentum strategy detects volatility contractions when Bollinger Bands fall inside Keltner Channels and waits for a breakout accompanied by strong momentum. Momentum strength is assessed using a standard deviation based threshold. Optional RSI and EMA trend filters refine entries. ATR can be used to set dynamic stop-loss and take-profit levels, and positions are closed after a time-based holding period.

## Details

- **Entry Criteria**:
  - Squeeze releases (Bollinger Bands outside Keltner Channels).
  - **Long**: Momentum > dynamic threshold, RSI crosses above oversold, trend EMA rising (optional).
  - **Short**: Momentum < -dynamic threshold, RSI crosses below overbought, trend EMA falling (optional).
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal, ATR-based stop-loss/take-profit, or time-based exit.
- **Stops**: Optional ATR stop-loss and take-profit.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0
  - `KeltnerPeriod` = 20
  - `KeltnerMultiplier` = 1.5
  - `MomentumLength` = 12
  - `TrendMaLength` = 50
  - `UseAtrStops` = True
  - `AtrMultiplierSl` = 1.5
  - `AtrMultiplierTp` = 2.5
  - `AtrLength` = 14
  - `MinVolatility` = 0.5
  - `HoldingPeriodMultiplier` = 1.5
  - `UseTrendFilter` = True
  - `UseRsiFilter` = True
  - `RsiLength` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `MomentumMultiplier` = 1.5
  - `AllowLong` = True
  - `AllowShort` = True
- **Filters**:
  - Category: Volatility breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Keltner Channels, Momentum, RSI, EMA, ATR
  - Stops: Optional
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
