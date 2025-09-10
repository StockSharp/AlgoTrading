# Breadth Thrust Strategy with Volatility Stop-Loss
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades when market breadth surges. It calculates a breadth ratio from advancing and declining stocks and optionally combines advancing and declining volume. The ratio is smoothed by a moving average, and a long position is opened when the smoothed value crosses above a low threshold. Risk is managed with an ATR-based trailing stop and a fixed holding period.

## Details
- **Entry**: smoothed breadth ratio crosses above `Low Threshold`.
- **Exit**:
  - price hits trailing stop based on `Volatility Multiplier * ATR`.
  - position has been held for `Hold Periods` candles.
- **Parameters**:
  - `Smoothing Length` – SMA period.
  - `Low Threshold` – breadth trigger level.
  - `Use Volume` – include volume ratio.
  - `Hold Periods` – number of candles to hold trade.
  - `Volatility Multiplier` – ATR multiplier for stop.
  - `Candle Type` – timeframe for all securities.
