# 1H EUR/USD MACD Swing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the "1H EUR_USD" MetaTrader expert advisor into the StockSharp high-level API. It trades the EUR/USD pair on hourly candles using dual moving averages and MACD swing detection. Entries require both a trend filter (fast MA above/below slow MA) and a MACD double-bottom/double-top pattern combined with a breakout of recent highs or lows. Risk is controlled with pip-based stop loss, take profit, and an incremental trailing stop that mirrors the original EA logic.

## Details

- **Market**: Designed for EUR/USD on the 1-hour timeframe but can be applied to any instrument producing standard candles.
- **Entry Criteria**:
  - **Long**:
    - Fast MA is above the slow MA (type selectable between SMA, EMA, SMMA, LWMA).
    - MACD main line forms either of the following bullish swings entirely below the zero line:
      - `MACD[-1] > MACD[-2] < MACD[-3]` with `MACD[-2] < 0` and the current close breaks the previous candle high.
      - `MACD[-2] > MACD[-3] < MACD[-4]` with `MACD[-3] < 0` and the current close breaks the high from two candles ago.
  - **Short**:
    - Fast MA is below the slow MA.
    - MACD main line forms the mirrored bearish swings entirely above the zero line and price closes below the relevant prior low.
- **Exit Criteria**:
  - Pip-based take profit and stop loss are attached immediately after entry.
  - Trailing stop activates only after price moves in favor by `TrailingStop + TrailingStep` pips and then follows price at a distance of `TrailingStop` pips, matching the EA's stepwise modification logic.
  - Protective orders trigger on the candle's intraperiod high/low.
- **Position Management**:
  - Uses the configured trade volume; reversing positions closes the opposite side before opening the new one.
  - Long and short trades share the same pip calculations (pip size automatically adapts to 4/5-digit quotes).
- **Indicators**:
  - Fast and slow moving averages with selectable type (Simple, Exponential, Smoothed, Linear Weighted) and optional horizontal shift.
  - Classic MACD (fast/slow/signal EMA lengths).
- **Parameters**:
  - `TradeVolume` – base lot size sent with each order.
  - `StopLossPips`, `TakeProfitPips` – protective distances in pips (set to zero to disable).
  - `TrailingStopPips`, `TrailingStepPips` – trailing configuration; trailing step must remain positive when trailing is active.
  - `FastMaLength`, `FastMaShift`, `FastMaType` – fast MA settings.
  - `SlowMaLength`, `SlowMaShift`, `SlowMaType` – slow MA settings.
  - `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACD parameters.
  - `CandleType` – timeframe for processing (defaults to 1 hour).
  - `LookbackPeriod` – preserved for compatibility with the MQL inputs; it does not alter logic because the original EA also left it unused.

## Notes

- Trailing stop behaviour mirrors the MQL version: no adjustment occurs until both the trailing distance and trailing step are surpassed by unrealized profit.
- The strategy assumes price step equals the quote point; if the instrument has 3 or 5 decimal digits the code automatically scales pip size by 10.
- Comments inside the C# source explain every key block in English for easier maintenance and extension.
