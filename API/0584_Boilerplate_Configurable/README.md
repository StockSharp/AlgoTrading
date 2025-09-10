# Boilerplate Configurable Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Boilerplate Configurable strategy can switch between two modes: a simple moving average crossover or a Bollinger squeeze breakout. It features trading day and session filters, a date range, a news window, and risk management using ATR or static risk/reward.

## Details

- **Entry Criteria**:
  - In `SmaCross` mode, go long when the fast SMA crosses above the slow SMA and go short on the opposite cross.
  - In `Squeeze` mode, enter when price breaks the outer Bollinger band while remaining inside the narrower band.
- **Long/Short**: Configurable for long, short, or both with optional inversion.
- **Exit Criteria**:
  - Stop loss and take profit based on ATR or static percentages.
  - Daily exit period and news window close all positions.
- **Stops**: Per-trade stop loss and take profit with drawdown protection.
- **Default Values**:
  - `Length` = 20
  - `WideMultiplier` = 1.5
  - `NarrowMultiplier` = 2
  - `MaxLossPerc` = 0.02
  - `AtrMultiplier` = 1.5
  - `StaticRr` = 2
  - `NewsWindow` = 5
  - `MaxDrawdown` = 0.1
- **Filters**:
  - Category: Modular
  - Direction: Long & Short
  - Indicators: SMA, Bollinger Bands, ATR
  - Stops: Yes
  - Complexity: High
  - Timeframe: Any
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: High
