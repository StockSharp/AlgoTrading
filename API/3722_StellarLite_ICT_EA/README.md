# StellarLite ICT EA Strategy

## Overview
StellarLite ICT EA is a discretionary-style algorithm that translates the "Stellar Lite" prop-firm playbook into StockSharp. The strategy merges two Inner Circle Trader (ICT) entry models — Silver Bullet and the 2022 model — and automates the partial take-profit plan used in the original MetaTrader expert advisor. It works on any instrument that provides candle, price step, and volume step information.

## Core Workflow
1. **Directional bias from the higher timeframe** – a moving average on the selected higher timeframe must slope in the trade direction and price must close beyond the average. Only after the bias is confirmed will the lower timeframe logic be evaluated.
2. **Liquidity sweep confirmation** – the strategy monitors a configurable lookback window and looks for breaks of recent highs or lows. Silver Bullet requires a sweep in the trade direction, while the 2022 model requires an inducement sweep in the opposite direction.
3. **Market Structure Shift (MSS)** – the last three finished candles must confirm a shift: a higher close above the previous high for long trades or a lower close beneath the previous low for short trades.
4. **Fair Value Gap (FVG) detection** – the strategy scans the most recent ten candles for bullish or bearish imbalances created by displacement candles. Entry is only allowed when the current close is inside the detected gap.
5. **NDOG / NWOG filter** – the present candle must be a narrow-range bar. Its high-low range may not exceed `AtrThreshold` multiplied by the `AverageTrueRange` value.
6. **Entry, stop, and targets** – the entry price is placed either at the middle of the gap or at the OTE (Optimal Trade Entry) retracement defined by the Fibonacci ratio parameter. The protective stop is located beyond recent swing liquidity, and three take-profit levels are projected using the configured risk-reward ratios.
7. **Trade management** – the position is sized according to the selected risk percentage or falls back to the strategy volume. When TP1, TP2, and TP3 are hit the strategy closes 50%, 25%, and 25% of the position by default, moves the stop to break-even after TP1 (with an optional offset), activates a trailing stop after TP2, and liquidates the rest at TP3 or upon a stop hit.

## Parameters
- **Entry Candle (`CandleType`)** – lower timeframe candles used for entry signals.
- **Higher Timeframe (`HigherTimeframeType`)** – candles feeding the bias moving average.
- **Higher MA Period (`HigherMaPeriod`)** – moving average length for bias detection.
- **ATR Period (`AtrPeriod`)** – lookback for the ATR consolidation filter.
- **Liquidity Lookback (`LiquidityLookback`)** – number of candles inspected to locate liquidity pools.
- **ATR Threshold (`AtrThreshold`)** – maximum allowed candle range as a fraction of ATR.
- **TP1/TP2/TP3 Risk Reward (`Tp1Ratio`, `Tp2Ratio`, `Tp3Ratio`)** – risk-reward multipliers for targets.
- **TP1/TP2/TP3 Close % (`Tp1Percent`, `Tp2Percent`, `Tp3Percent`)** – partial close percentages.
- **Break Even After TP1 (`MoveToBreakEven`)** – toggles the break-even adjustment.
- **Break Even Offset (`BreakEvenOffset`)** – number of price steps added or subtracted when moving the stop.
- **Trailing Distance (`TrailingDistance`)** – trailing stop distance (in price steps) activated after TP2.
- **Use Silver Bullet / Use 2022 Model (`UseSilverBullet`, `Use2022Model`)** – enable or disable each setup.
- **Use OTE Entry (`UseOteEntry`)** – calculate the entry inside the optimal trade entry zone.
- **Risk % (`RiskPercent`)** – percentage of equity risked per trade to derive the position size.
- **OTE Lower (`OteLowerLevel`)** – Fibonacci coefficient for the OTE level.

## Practical Notes
- The strategy requires finished candles; ensure the data feed supplies close prices and volume steps.
- Position sizing falls back to the strategy `Volume` parameter when the portfolio value or tick value information is unavailable.
- Liquidity detection and MSS logic rely on the most recent history cache (20 candles by default); allow the strategy to collect enough data before expecting signals.
- Partial exits respect the instrument volume step; if the requested fraction is smaller than the minimum tradable volume the close is skipped.
- Trailing logic keeps updating the stop only in the profit direction and never loosens existing risk controls.

## Files
- `CS/StellarLiteIctEaStrategy.cs` – implementation of the StockSharp strategy.
- `README.md` – English documentation.
- `README_cn.md` – Simplified Chinese documentation.
- `README_ru.md` – Russian documentation.
