# Cyberia Trader Adaptive Strategy

## Overview
The **Cyberia Trader Adaptive Strategy** is a C# port of the classic MetaTrader "CyberiaTrader" expert advisor. The
strategy rebuilds the original probability driven core in StockSharp and augments it with optional technical filters.
It continuously analyses price swings to measure the odds of reversals and then optionally confirms the signal with EMA,
MACD, CCI, ADX or fractal filters before sending orders.

## Probability engine
The heart of the strategy is the probability calculator inspired by the MQL version. It uses an adaptive sampling period
(`ValuePeriod`) and inspects historical bars at fixed steps to classify each bar as:

* **Sell probability** – bullish bar following a bearish bar (potential fading opportunity).
* **Buy probability** – bearish bar following a bullish bar.
* **Undefined probability** – all other bar configurations.

For each class the strategy accumulates average amplitude, hit-rate and success-rate statistics over `ValuePeriod × HistoryMultiplier`
samples. The adaptive search scans periods from `1` to `MaxPeriod` (default 23) and keeps the period that produces the highest
success-rate. These statistics are exposed internally as:

* `BuyPossibility`, `SellPossibility`, `UndefinedPossibility` – current bar classification values.
* `BuyPossibilityMid`, `SellPossibilityMid`, ... – running averages used by the original decision tree.
* `PossibilityQuality`, `PossibilitySuccessQuality` – quality ratios used for diagnostics and auto period selection.

When insufficient history is available the strategy simply waits until the probability engine reports a valid sample set.

## Indicator filters
The original EA allowed enabling or disabling additional indicator based modules. The port keeps the same idea:

* **EMA filter** – compares the slope of an EMA (`MaPeriod`) between the last two finished candles.
* **MACD filter** – checks the relation between MACD and its signal line (`MacdFast`, `MacdSlow`, `MacdSignal`).
* **CCI filter** – flags overbought/oversold regimes using `CciPeriod` and ±100 thresholds.
* **ADX filter** – inspects +DI and −DI components (`AdxPeriod`) to prefer the dominant direction.
* **Fractal filter** – detects the most recent swing using a configurable `FractalDepth` window and blocks orders against it.
* **Reversal detector** – toggles the direction flags when a probability spike exceeds `ReversalIndex` times its average.

Each module can be toggled via parameters and mirrors the behaviour of the original boolean extern inputs.

## Trading logic
1. Subscribe to the configured candle series (`CandleType`).
2. Rebuild the probability statistics and optionally re-select the optimal sampling period on every finished candle.
3. Apply the optional indicator filters and the Cyberia decision tree to enable or disable buy/sell directions.
4. Execute trades when a buy or sell decision is active, respecting the global `BlockBuy` and `BlockSell` switches.
5. Optionally apply absolute stop-loss or take-profit protection if `StopLossPoints` or `TakeProfitPoints` are specified.
6. Close positions early when the decision becomes `Unknown` and the probability quality deteriorates.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle series used for calculations. |
| `AutoSelectPeriod` | Enables the adaptive search over `MaxPeriod` to find the best sampling window. |
| `InitialPeriod` | Fallback probability period when auto selection is disabled. |
| `MaxPeriod` | Maximum period considered during the adaptive search (default 23 like the EA). |
| `HistoryMultiplier` | Number of samples per period used in the statistics (default 5). |
| `SpreadFilter` | Minimum move (in price units) required to treat a probability as "successful". |
| `EnableCyberiaLogic` | Toggles the original decision tree that compares probability averages. |
| `EnableMa`, `EnableMacd`, `EnableCci`, `EnableAdx`, `EnableFractals`, `EnableReversalDetector` | Enable individual filters. |
| `MaPeriod` | EMA length for the moving-average filter. |
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD configuration. |
| `CciPeriod` | Commodity Channel Index length. |
| `AdxPeriod` | Average Directional Index length. |
| `FractalDepth` | Odd number of candles analysed to detect the most recent fractal swing. |
| `ReversalIndex` | Multiplier used by the reversal detector. |
| `BlockBuy`, `BlockSell` | Hard switches that stop opening trades in the given direction. |
| `TakeProfitPoints`, `StopLossPoints` | Optional absolute take-profit and stop-loss distances. |

## Notes
* The adaptive period search requires sufficient history: `ValuePeriod × HistoryMultiplier + ValuePeriod` bars.
* All comments were rewritten in English and the logic keeps to the high level StockSharp API with indicator bindings.
* The probability metrics are internal fields but exposed through logs or by extending the strategy if further diagnostics are needed.
