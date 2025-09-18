# FiveMinutesScalpingEA v1.1 (StockSharp port)

## Overview
The **FiveMinutesScalpingEaV11Strategy** is a conversion of the MetaTrader 4 expert advisor *5MinutesScalpingEA v1.1*. The strategy keeps the original concept of combining multi-period Hull moving averages, a momentum Fisher transform, an ATR breakout detector and a trend filter to scalp short-lived movements on a five-minute chart. The implementation follows the StockSharp high level API and uses candle subscriptions with indicator bindings to reproduce the expert advisor behaviour.

The strategy is designed for single-symbol trading. Only one net position is maintained at any time and all signals are evaluated on completed candles. Protective orders are simulated inside the strategy by monitoring candle highs and lows.

## Indicator stack
| Component | StockSharp implementation | Purpose |
|-----------|--------------------------|---------|
| `i1` custom Hull MA | `HullMovingAverage` with period `Period1` (default 30) | Detects fast trend direction via the slope of the Hull moving average. |
| `i2` custom Hull MA | `HullMovingAverage` with period `Period2` (default 50) | Confirms broader trend direction; both Hull filters must agree for entries in normal mode. |
| `i3` Fisher momentum | `FisherTransform` with period `Period3` | Acts as a momentum oscillator. Positive values favour long setups, negative values favour short setups. |
| `i4` ATR breakout arrows | `AverageTrueRange` with period `Period4` combined with candle comparisons | Searches for strong breakouts where the current high/low exceeds the previous two highs/lows by at least one ATR. |
| `i5` Fisher trend filter | `FisherTransform` with period `Period5` | Provides a smoothed trend confirmation similar to the original EA trend histogram. |

For each indicator the strategy stores historical values so that it can read the value `IndicatorShift` candles back, matching the MQL4 `IndicatorsShift` parameter. All filters can be disabled individually through their respective parameters.

## Trading logic
1. The strategy subscribes to the candle series defined by `CandleType` (default: 5-minute candles).
2. On every finished candle the Hull, Fisher and ATR indicators are updated. When enough history is available, the strategy evaluates the candle that is `IndicatorShift` bars back.
3. **Normal mode** (`SignalMode = Normal`):
   - A **long** entry requires all enabled filters to report bullish conditions (positive Hull slope, Fisher momentum above zero, ATR breakout upwards, trend Fisher above zero).
   - A **short** entry requires all enabled filters to report bearish conditions (negative Hull slope, Fisher momentum below zero, ATR breakout downwards, trend Fisher below zero).
4. **Reverse mode** (`SignalMode = Reverse`) simply swaps the interpretation of bullish and bearish conditions.
5. A new signal flips the internal `_lastSignal` flag. If `CloseOnSignal` is enabled the opposite position is closed immediately before a new entry is sent.
6. The parameter `UseTimeFilter` restricts entries to the `[StartHour, EndHour)` range (with wrap-around behaviour identical to the MQL4 EA).

## Risk management
The StockSharp port implements the following protective features:
- **Stop loss / take profit** – If enabled, stop and target prices are placed at a fixed distance (`StopLossPips`, `TakeProfitPips`) from the entry price and monitored on every candle.
- **Trailing stop** – When `UseTrailingStop` is enabled a trailing anchor is maintained. Once price advances by `TrailingStepPips`, the stop is moved so that it remains `TrailingStopPips` away from the current extreme.
- **Break-even** – If `UseBreakEven` is enabled and price moves by `BreakEvenPips + BreakEvenAfterPips`, the stop is tightened to `BreakEvenPips` away from the entry.
- **Single position** – All exits are executed via market orders (`SellMarket` / `BuyMarket`) which close the entire net position.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | M5 | Primary timeframe. |
| `IndicatorShift` | 1 | Number of closed candles to look back when evaluating filters. |
| `SignalMode` | Normal | Use normal or reversed signals. |
| `UseIndicator1`..`UseIndicator5` | true | Toggles each filter. |
| `Period1`, `Period2`, `Period3`, `Period4`, `Period5` | 30, 50, 10, 14, 18 | Periods for Hull, Fisher and ATR calculations. |
| `PriceMode3` | HighLow | Compatibility parameter for the original Fisher price selection. The StockSharp implementation always feeds the default candle price to the Fisher indicator. |
| `CloseOnSignal` | false | Close the opposite position when a new entry signal appears. |
| `UseTimeFilter`, `StartHour`, `EndHour` | false, 0, 0 | Optional intraday trading window. |
| `UseTakeProfit`, `TakeProfitPips` | true, 10 | Take profit management. |
| `UseStopLoss`, `StopLossPips` | true, 10 | Stop loss management. |
| `UseTrailingStop`, `TrailingStopPips`, `TrailingStepPips` | false, 1, 1 | Trailing stop management. |
| `UseBreakEven`, `BreakEvenPips`, `BreakEvenAfterPips` | false, 4, 2 | Break-even stop logic. |
| `TradeVolume` | 0.01 | Volume for market entries. |

## Differences vs. original EA
- Basket close logic (`UseBasketClose`, `CloseInProfit`, `CloseInLoss`) is not implemented because the StockSharp strategy works with a single net position.
- Automatic lot sizing (`AutoLotSize` / `RiskFactor`) and spread checks are not part of this port. Use the hosting environment to control volume and slippage.
- The Fisher price mode parameter is exposed for compatibility but the StockSharp `FisherTransform` currently uses the default candle price. Other price modes can be emulated by extending the indicator if required.
- Trade management is performed on completed candles, which mirrors the EA behaviour when `IndicatorsShift >= 1`.

## Usage tips
1. Attach the strategy to a liquid instrument with tight spreads (the EA was originally designed for EUR/USD M5).
2. Configure `TradeVolume` according to your account sizing rules.
3. Adjust indicator periods or disable filters to match your risk tolerance.
4. Combine with the built-in time filter to avoid low-liquidity sessions.
5. Always validate settings in the StockSharp tester before running on live data.
