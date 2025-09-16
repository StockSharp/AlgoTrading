# WAMI Cloud X2
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the dual timeframe behaviour of the original MetaTrader "Exp_WAMI_Cloud_X2" expert advisor. It uses the Warren Momentum Indicator (WAMI) on a higher timeframe to define the dominant bias and a second instance of the same indicator on a lower timeframe to time entries and exits. The main WAMI line is compared against its internal signal line on both timeframes, which mirrors the logic of the original MQL implementation.

## Concept

- **WAMI construction** – WAMI is built from the first difference of closing prices, smoothed by three sequential moving averages with individually selectable methods (SMA, EMA, SMMA or LWMA). A fourth moving average produces the signal line. The custom indicator in the strategy reproduces this chain exactly, so both main and signal lines are available in one value payload.
- **Trend filter (higher timeframe)** – The default six-hour candles drive the trend WAMI. Whenever the main line is above the signal line the trend direction becomes bullish; below it becomes bearish. A neutral state is kept when both lines are equal or the indicator is still forming.
- **Signal engine (lower timeframe)** – The default 30-minute candles are used to search for entries. For every finished candle the strategy stores recent WAMI values and evaluates the last closed bar defined by `SignalBar`. Crossings are detected by comparing the most recent value (`SignalBar`) against the previous one (`SignalBar + 1`).

## Trading Rules

1. **Exits**
   - Long positions are closed when the signal timeframe shows persistent bearishness (`previous.Main < previous.Signal`) if `CloseLongOnSignal` is enabled.
   - Short positions are closed analogously when `CloseShortOnSignal` is enabled.
   - When the higher timeframe flips direction (`_trendDirection`), the respective `CloseLongOnTrendFlip` or `CloseShortOnTrendFlip` flag forces an exit.
2. **Entries**
   - Short entries are allowed when the higher timeframe is bearish and the signal WAMI crosses upward (`current.Main >= current.Signal` with `previous.Main < previous.Signal`). This matches the original EA that sells on the first upward penetration of the signal line within a downtrend.
   - Long entries are the mirrored condition when the higher timeframe is bullish and the signal WAMI crosses downward (`current.Main <= current.Signal` with `previous.Main > previous.Signal`).
   - Entry toggles (`EnableBuyEntries`, `EnableSellEntries`) can disable either side. When an opposite position is open, the strategy sends a compensating market order to flatten and reverse in a single command, just like the MQL helper functions.

## Parameters

- **Trend WAMI** – `TrendPeriod1/2/3`, `TrendMethod1/2/3`, `TrendSignalPeriod`, `TrendSignalMethod`, `TrendCandleType`.
- **Signal WAMI** – `SignalPeriod1/2/3`, `SignalMethod1/2/3`, `SignalSignalPeriod`, `SignalSignalMethod`, `SignalCandleType`.
- **Control Flags** – `SignalBar`, `EnableBuyEntries`, `EnableSellEntries`, `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`, `CloseLongOnSignal`, `CloseShortOnSignal`.
- **Trading Size** – `TradeVolume` defines the market order size used for new entries. Reversals send the opposite volume plus the configured size.

All parameters are exposed through `StrategyParam<T>` objects, so they can be optimised or modified from the StockSharp UI just as the MetaTrader inputs allowed.

## Default Values

- **Trend timeframe** – 6-hour candles.
- **Signal timeframe** – 30-minute candles.
- **All moving average methods** – Simple (SMA).
- **Moving average lengths** – 4 / 13 / 13 for the three stages and 4 for the signal line on both timeframes.
- **SignalBar** – 1 (use the last closed candle).
- **TradeVolume** – 1 contract.
- **All permission flags** – Enabled (true).

## Additional Notes

- The strategy does not set hard stop-loss or take-profit orders. Risk management should be configured externally if required.
- Chart helpers draw the signal timeframe candles, both WAMI lines and the executed trades. The trend timeframe is plotted in a separate area for visual confirmation.
- The implementation avoids indicator value polling (no `GetValue` calls) and sticks to the high-level candle subscription API, following the project guidelines.
