# TradePad Sample Strategy

## Overview

The **TradePad Sample Strategy** is a port of the MetaTrader "TradePad" example. The original expert adviser rendered a grid of
buttons that displayed the short-term trend for multiple symbols by coloring each cell with the current Stochastic oscillator
reading. This StockSharp implementation keeps the analytical core of the sample and focuses on monitoring a list of instruments
without replicating the on-chart user interface. The strategy subscribes to candle data for every configured symbol, calculates a
Stochastic oscillator, and classifies each instrument into *Uptrend*, *Downtrend*, or *Flat* states. Every time the class changes,
the strategy writes a log message similar to the color change performed by the original TradePad.

The strategy does not place orders. Its goal is to help discretionary traders keep track of several markets at once and spot
momentum changes that require manual actions (for example, switching charts or preparing trades).

## How It Works

1. **Symbol discovery** – the `SymbolList` parameter accepts a comma-separated list of tickers. If no list is supplied, the
   strategy falls back to the main `Security` assigned in the runner.
2. **Candle subscription** – each symbol uses the same timeframe configured through `CandleType`.
3. **Indicator processing** – a dedicated `StochasticOscillator` instance is bound to the candle stream. When the candle is
   finished the indicator produces the `%K` value used for trend classification.
4. **Trend classification** – a reading above `UpperLevel` maps to *Uptrend*, a reading below `LowerLevel` maps to *Downtrend*,
   everything in between is *Flat*. The last oscillator value is stored in `LatestKValues`.
5. **Refresh interval** – the strategy mimics the timer behaviour of the original TradePad. A change is logged at most once per
   `TimerPeriodSeconds` for each symbol even if multiple candles arrive within the interval.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `SymbolList` | Comma-separated list of instruments to monitor. Empty string means "use the main security". |
| `TimerPeriodSeconds` | Minimum number of seconds between state updates per symbol. Prevents log spam when candles are very short. |
| `StochasticLength` | Lookback period used to compute the raw `%K` line. |
| `StochasticKPeriod` | Smoothing period applied to the `%K` line. |
| `StochasticDPeriod` | Smoothing period applied to the `%D` line (kept for completeness although the strategy only reads `%K`). |
| `UpperLevel` | Threshold above which the symbol is considered to be in an uptrend. |
| `LowerLevel` | Threshold below which the symbol is considered to be in a downtrend. |
| `CandleType` | Timeframe of the candles used for indicator calculation. |

## Usage Notes

- Ensure the specified tickers are available from the connector; missing symbols are reported in the log and skipped.
- The `TrendStates` property exposes the latest classification for external dashboards or Designer blocks.
- Use the strategy inside Designer or Runner to attach your own visuals (dashboards, charts) that react to the `AddInfoLog`
  messages or the public dictionaries.
- Because no orders are sent, the strategy is safe to run on live data providers purely for monitoring purposes.

## Original MQL Behavior vs. StockSharp Version

| MQL5 Feature | StockSharp Implementation |
|--------------|--------------------------|
| Graphical grid of buttons | Exposed as log entries and public dictionaries so that custom UI can be built in Designer. |
| Manual BUY/SELL buttons | Not implemented; the strategy intentionally stays passive. |
| Chart dragging logic | Not applicable in StockSharp and omitted. |
| Trend color updates | Replaced with trend state changes triggered every `TimerPeriodSeconds` per symbol. |

## Extending the Strategy

- Connect the `TrendStates` dictionary to Designer widgets to rebuild the colored pad using XAML controls.
- Add alerts or notifications when a symbol transitions from *Flat* to *Uptrend* or *Downtrend*.
- Combine the classification with order logic if you want to automate entries after identifying strong momentum.
