# Artificial Intelligence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Accelerator Oscillator (AC) values as inputs to a simple linear perceptron. Four AC readings spaced seven bars apart are weighted by user-defined coefficients. A positive perceptron output opens a long position, and a negative output opens a short.

The strategy always applies a stop-loss. If an opposite signal appears after profit exceeds twice the stop-loss, the position reverses with increased volume. Otherwise, the stop-loss moves to break-even.

## Details

- **Entry Criteria**:
  - **Long**: Perceptron output > 0.
  - **Short**: Perceptron output < 0.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal with profit > 2 * StopLoss → reverse.
  - Opposite signal with smaller profit → stop moved to entry.
  - Stop-loss hit.
- **Stops**: Fixed stop-loss in points.
- **Filters**: None.

## Parameters
- `StopLoss` – stop-loss distance in points (default 850).
- `Shift` – bar shift for indicator values (default 1).
- `X1`, `X2`, `X3`, `X4` – perceptron weights.
- `CandleType` – candle timeframe.
