# RSI RFTL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **RSI RFTL EA** from MetaTrader 5 to the StockSharp high-level API. It keeps the original idea of trading RSI swing trendlines, enhanced with the Recursive Filter Trend Line (RFTL) as a directional filter. The implementation reproduces the bar-by-bar decision making of the expert advisor while using idiomatic StockSharp constructs such as `StrategyParam`, indicator bindings and candle subscriptions.

## How It Works

1. **RSI swing detection** – the latest 500 RSI values are scanned for local highs and lows. Peaks must rise above 40 and 60, while troughs must fall below 60 and 40, matching the MQL turning-point logic.
2. **Trendline projection** – once two valid highs or lows are found, the strategy projects the corresponding RSI trendline to the current bar and to the prior bar. Intermediate swings that break the 40/60 thresholds invalidate the line, just as in the expert advisor.
3. **RFTL confirmation** – the previous value of the Recursive Filter Trend Line (calculated with the original coefficient table) must sit above the previous close for shorts or below it for longs. This keeps entries aligned with the RFTL filter.
4. **Entry gating** – RSI must also sit on the appropriate side of neutral: shorts require RSI to stay above 47/50, while longs require RSI to remain below 55/50.
5. **Risk layer** – protective stop, take-profit and trailing-stop distances are expressed in pips and updated on every finished candle, mimicking the MQL trailing modification routine. Additional exits fire when RSI exceeds 70 (close longs) or drops below 30 (close shorts).

## Entry Logic

- **Short setup**
  - Two RSI lows below 60/40 define a rising trendline whose projection is now broken to the downside (`RSI[1] < line`, `RSI[2] > line(previous)`).
  - Previous RFTL value is above the previous close, confirming downward pressure.
  - RSI stays on the bullish side (`RSI[2] > 50`, `RSI[0] > 47`) and the detected tops lie farther in history than the lows (`pos₂ > pos₄`), matching the MQL ordering constraint.
- **Long setup**
  - Two RSI highs above 40/60 define a falling trendline whose projection is now broken to the upside (`RSI[1] > line`, `RSI[2] < line(previous)`).
  - Previous RFTL value is below the previous close.
  - RSI remains on the bearish side (`RSI[2] < 50`, `RSI[0] < 55`) and the recent lows are more recent than the highs (`pos₄ > pos₂`).

Signals are evaluated only after all indicators are formed and the necessary history is accumulated, preventing premature trades on partial data.

## Risk Management

- **Stop Loss / Take Profit** – configurable in pips. If the current candle trades beyond the respective price level, the position is closed immediately and trailing state is reset.
- **Trailing Stop** – optional. Once price moves by `TrailingStopPips + TrailingStepPips` in favour of the trade, the stop trails the close while enforcing the same minimum advance (`TrailingStepPips`) before tightening again.
- **RSI Emergency Exit** – longs close when RSI crosses 70; shorts close when it falls below 30. This mirrors the hard exits coded in the original EA.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `CandleType` | 1 hour | Timeframe used for both RSI and RFTL calculations. |
| `TradeVolume` | 1 | Order volume submitted on each entry. |
| `RsiPeriod` | 30 | Lookback period of the RSI oscillator. |
| `StopLossPips` | 50 | Protective stop distance in pips (0 disables the stop). |
| `TakeProfitPips` | 50 | Take-profit distance in pips (0 disables the target). |
| `TrailingStopPips` | 5 | Trailing stop offset in pips (0 disables trailing). |
| `TrailingStepPips` | 5 | Additional pip improvement required before trailing updates. |

All distances are multiplied by the instrument `PriceStep`, matching the point/ pip handling of the MQL version.

## Usage

1. Attach the strategy to a security and set `CandleType` to the bar size used in your MetaTrader tests.
2. Adjust the risk parameters (stop, take, trailing) to the pip distances you used previously. Setting a parameter to `0` disables that protection.
3. Start the strategy; it will subscribe to the specified candles, compute RSI and RFTL, and begin monitoring signals once enough history is collected.
4. Monitor the chart widgets – the price area displays candles and the RFTL line, while the second pane shows the RSI oscillator.

## Notes & Differences

- The RFTL indicator is implemented directly in C# with the original coefficient table; no external files are required.
- Trade management stays single-position: the strategy flips between long, short and flat just like the EA that only tracked one position per symbol/magic.
- Because stop and trailing exits are handled inside the strategy (StockSharp does not auto-execute MT5 stops), re-entries are skipped on the bar where a protective exit fires, which is a conservative but safe approximation.
- History buffers are capped at 600 records to mirror the 500-element arrays used in the source code while avoiding unbounded memory growth.
- All inline comments were rewritten in English and the code follows the StockSharp high-level API style guidelines.
