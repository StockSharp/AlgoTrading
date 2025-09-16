# Statistics Repeating Behavior Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Intraday strategy that studies how candles behaved at the same time of day during the last N trading sessions. For every new bar it compares the accumulated bullish and bearish body sizes from previous days. If bullish pressure dominates it opens a long position at the bar open, otherwise it goes short. Positions are closed on the next bar and a fixed pip stop loss mimics the original MetaTrader logic. Position size follows a golden-ratio martingale by growing after losses and resetting after wins.

## Trading Logic

1. At the start of each new candle, close any open position from the previous bar.
2. Look up candles from the last `HistoryDays` trading days that opened at the same hour and minute.
3. Sum the candle bodies (in points) separately for bullish and bearish closes, ignoring bodies smaller than `MinimumBodyPoints`.
4. If bullish sum exceeds bearish sum → open a long position using the current volume.
5. If bearish sum exceeds bullish sum → open a short position.
6. Apply a stop loss of `StopLossPips` converted through the instrument price step. The stop is checked against intrabar extremes when the candle finishes.
7. When the trade closes:
   - If the result is profitable, reset volume back to `InitialVolume`.
   - Otherwise multiply the current volume by `MartingaleFactor` (respecting volume step and limits).

## Parameters

- **HistoryDays** *(default: 10)* — number of previous days to include in the statistics.
- **MinimumBodyPoints** *(default: 10)* — candles with a body smaller than this threshold (in points) are ignored.
- **StopLossPips** *(default: 15)* — pip distance of the protective stop.
- **InitialVolume** *(default: 0.1)* — starting order size before martingale adjustments.
- **MartingaleFactor** *(default: 1.618)* — multiplier applied after a losing trade.
- **CandleType** *(default: 1 hour)* — timeframe used for candles.

## Trading Characteristics

- **Market Side**: Both long and short depending on statistics.
- **Timeframe**: Configurable (default hourly) with exact matching by hour and minute.
- **Position Management**: Single position at a time, closed on the next bar or when stop loss is hit.
- **Risk**: Uses fixed pip stop and martingale sizing, which can grow volume quickly after consecutive losses.
- **Instruments**: Works with instruments that provide a valid `MinPriceStep` and volume limits.

## Implementation Notes

- Candle bodies are stored per minute-of-day in a rolling queue capped by `HistoryDays`.
- Volumes are normalized to the instrument volume step and bounded by `MinVolume`/`MaxVolume`.
- Stop loss detection relies on completed candle extremes to emulate intrabar execution from the original MQL5 expert.
- All inline code comments are provided in English to align with repository requirements.
