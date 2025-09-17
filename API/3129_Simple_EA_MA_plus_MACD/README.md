# Simple EA MA plus MACD
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
This strategy ports the MetaTrader 5 expert adviser **Simple EA MA plus MACD** to the StockSharp high-level API. It searches for a breakout from a “signal bar” that satisfies two conditions: a shifted moving average sits below/above the bar’s highs, and the MACD histogram has just crossed the zero line. When the next candle closes beyond the signal bar’s extremum the strategy enters in the breakout direction.

The implementation keeps the original behaviour of the EA:

1. **Signal detection** – on every finished candle the strategy inspects the previous bar. A configurable moving average (default LWMA) calculated on the chosen applied price must be lower than both the previous and current candle highs for longs (higher for shorts). Simultaneously the MACD main line must have crossed zero between the two preceding bars.
2. **Signal confirmation** – once a signal bar is stored, the strategy waits for the next completed candle. A close above the stored high triggers a long breakout; a close below the stored low triggers a short breakout. If price invalidates the signal by closing back inside the signal bar, the setup is cancelled.
3. **Position management** – newly opened trades inherit stop-loss, take-profit and trailing-stop distances expressed in pips. Protective levels are converted to absolute prices using the security `PriceStep`. Instruments with three or five decimals receive the classic forex adjustment (step × 10) to mimic MetaTrader pip definitions.

## Risk management
- **Stop-loss / take-profit** – optional distances defined in pips are evaluated on every candle close. When the market prints beyond the corresponding level the strategy exits with a market order.
- **Trailing stop** – when profit exceeds `TrailingStopPips + TrailingStepPips`, a trailing reference is moved behind the best price reached. If price pulls back to the trailing level the position is closed. A trailing step of zero re-arms the stop on every new extreme.
- **Flatten on reversal** – if an opposite breakout appears while an opposite position is open, the strategy sends a single market order large enough to close the existing exposure and open the new trade in one shot.

## Implementation notes
- The moving average supports the same smoothing methods and applied price options as MetaTrader (Simple, Exponential, Smoothed, LinearWeighted and Close/Open/High/Low/Median/Typical/Weighted prices).
- `MaShift` reproduces the horizontal offset of the MetaTrader indicator by reading values from earlier bars before evaluating the breakout rules.
- MACD uses the built-in `MovingAverageConvergenceDivergence` indicator. Only the histogram (difference between fast and slow EMAs) is required; the signal line period is retained to stay faithful to the EA settings.
- Candle subscriptions and indicator processing rely exclusively on the StockSharp high-level API. No manual tick handling or indicator buffers are used.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | `1` | Order size for every breakout entry. |
| `TakeProfitPips` | `50` | Profit target distance expressed in pips (converted to absolute price using the security price step). Set to 0 to disable. |
| `StopLossPips` | `50` | Protective stop distance in pips. Set to 0 to disable. |
| `TrailingStopPips` | `5` | Trailing stop distance in pips that is locked in once price advances sufficiently. |
| `TrailingStepPips` | `5` | Minimum additional progress (in pips) before the trailing stop is advanced again. |
| `MaPeriod` | `100` | Length of the moving average used to validate the signal bar. |
| `MaShift` | `0` | Horizontal shift applied to the moving average, emulating the MetaTrader `ma_shift` parameter. |
| `MaMethod` | `LinearWeighted` | Moving average smoothing method (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaAppliedPrice` | `Weighted` | Price source fed into the moving average (Close, Open, High, Low, Median, Typical, Weighted). |
| `MacdFastPeriod` | `12` | Fast EMA period used in the MACD calculation. |
| `MacdSlowPeriod` | `26` | Slow EMA period used in the MACD calculation. |
| `MacdSignalPeriod` | `9` | Signal line smoothing period retained for parity with the original EA. |
| `MacdAppliedPrice` | `Weighted` | Applied price used when feeding values into MACD. |
| `CandleType` | `1 hour` time frame | Primary candle series analysed for signals and trade management. |

## Usage tips
- Tune the pip-based protections to match the tick size of the selected instrument; incorrect `PriceStep` values on the connector side will distort pip conversions.
- For highly volatile markets consider increasing `TrailingStepPips` to reduce premature exits, or decrease it to tighten trailing behaviour.
- Because trades are executed on closed candles, the breakout must persist until the bar completes; enabling smaller timeframes increases trading frequency but may introduce more noise.
