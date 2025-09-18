# AbcWsCci Strategy

## Overview
The **AbcWsCci Strategy** combines two classic Japanese candlestick reversal patterns — **Three White Soldiers** and **Three Black Crows** — with the **Commodity Channel Index (CCI)** indicator for confirmation. The system scans finished candles, measures the body size relative to a moving average baseline, and opens trades only when strong multi-candle momentum aligns with CCI extremes. Position exits are triggered when the CCI leaves the extreme zones, signalling that momentum is fading.

## Trading Logic
- Maintain a moving average of candle body sizes to qualify “long” candles.
- Detect the Three White Soldiers pattern (three consecutive strong bullish candles with rising midpoints).
- Detect the Three Black Crows pattern (three consecutive strong bearish candles with falling midpoints).
- Confirm bullish entries with CCI dropping below **-50** and bearish entries with CCI rising above **50**.
- Close long positions when CCI crosses out of the **-80** or **80** levels, and close short positions on the mirrored conditions.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CciPeriod` | Length of the CCI indicator used for confirmation. | 37 |
| `BodyAveragePeriod` | Number of candles in the moving average that defines the minimum “strong” body size. | 13 |
| `CandleType` | Candle time frame used for pattern detection. | 1 hour |

## Indicators
- **Commodity Channel Index (CCI)**: Evaluates momentum extremes for confirmation and exit signals.
- **Simple Moving Average of candle bodies**: Establishes the minimum candle size required for a valid pattern.

## Position Management
- Enter **long** when Three White Soldiers form and CCI is below -50 while no long position is active.
- Enter **short** when Three Black Crows form and CCI is above 50 while no short position is active.
- Exit **long** positions when CCI leaves the -80/80 band, indicating the bullish impulse is exhausted.
- Exit **short** positions when CCI leaves the +80/-80 band, signalling bearish momentum loss.

## Usage Notes
- The strategy is event-driven: only fully completed candles are processed.
- Works best on trending instruments where multi-candle momentum combined with oscillator extremes provides reliable signals.
- Consider combining with additional risk management rules (stop-loss, position sizing) depending on your trading environment.
