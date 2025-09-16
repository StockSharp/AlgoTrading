# MA + RSI Wizard Strategy

## Overview

This strategy is the StockSharp port of the MetaTrader 5 "MQL5 Wizard MA RSI" expert from folder `MQL/17489`. The original robot combines a moving average filter with an RSI filter and opens trades when the weighted sum of the filters crosses configurable thresholds. The C# version keeps the same structure while expressing the logic with StockSharp's high level API and modern risk management helpers.

The bot works on any instrument that provides OHLCV candles. It evaluates one moving average that can be lagged by a user defined number of bars and an RSI that can be fed with different price sources. Both indicators contribute to a composite score. A position is opened once the score exceeds the open threshold and closed when the opposite score reaches the close threshold. Optional distance, stop loss and take profit settings replicate the money management parameters of the original Expert Advisor.

## Indicators and scoring

* **Moving Average** – configurable period, method (simple, exponential, smoothed, linear weighted), price source and forward shift. When the closing price is above the shifted average the MA score equals 100, otherwise it is 0.
* **Relative Strength Index (RSI)** – configurable period and price source. The RSI contribution grows linearly from 0 when RSI = 50 to 100 when RSI = 100 for long signals, and mirrors the same behaviour for short signals.
* **Composite score** – the MA and RSI scores are weighted by `MaWeight` and `RsiWeight`. The final value is the weighted average `score = (maScore * MaWeight + rsiScore * RsiWeight) / (MaWeight + RsiWeight)` which stays inside the [0;100] interval just like in the MetaTrader version.
* **Price distance filter** – `PriceLevelPoints` defines the minimum distance between the candle close and the shifted moving average (converted to price using the instrument step). Signals closer than the threshold are ignored.

## Trade rules

1. Every finished candle updates the indicators and scores.
2. If the opposite score breaches `ThresholdClose`, the current position is closed at market.
3. Long entry – allowed when there is no long exposure, the long score is at least `ThresholdOpen`, the cooldown (`ExpirationBars`) has passed, and the price distance filter is satisfied. The order size equals `Volume + |Position|`, which automatically flips a short position if needed.
4. Short entry – symmetrical to the long logic.
5. Optional `StartProtection` applies stop loss and take profit using absolute price points.

## Risk management

The strategy activates `StartProtection` once it starts. Distances are defined in price points (`StopLevelPoints`, `TakeLevelPoints`) and are translated with the current `Security.PriceStep`. Both values can be set to zero to disable the corresponding protection. The cooldown parameter prevents immediate re-entries in the same direction, emulating the pending order expiration setting of the original EA.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Data series used for analysis. | 15-minute time frame |
| `ThresholdOpen` | Minimum weighted score to open a position. | 55 |
| `ThresholdClose` | Minimum opposite score to close a position. | 100 |
| `PriceLevelPoints` | Required distance between price and shifted MA (in points). | 0 |
| `StopLevelPoints` | Stop loss distance (points). | 50 |
| `TakeLevelPoints` | Take profit distance (points). | 50 |
| `ExpirationBars` | Cooldown in bars before re-entering in the same direction. | 4 |
| `MaPeriod` | Moving average period. | 20 |
| `MaShift` | Forward shift applied to the MA output (bars). | 3 |
| `MaMethod` | Moving average method (Simple, Exponential, Smoothed, LinearWeighted). | Simple |
| `MaAppliedPrice` | Price source for the MA. | Close |
| `MaWeight` | Weight assigned to the MA score. | 0.8 |
| `RsiPeriod` | RSI period. | 3 |
| `RsiAppliedPrice` | Price source for the RSI. | Close |
| `RsiWeight` | Weight assigned to the RSI score. | 0.5 |

## Notes

* The strategy runs strictly on finished candles and ignores partial updates.
* Setting both indicator weights to zero disables trading because the combined score can no longer reach the thresholds.
* Cooldown (`ExpirationBars`) equal to zero allows multiple entries in the same direction without waiting.
* Because StockSharp executes market orders by default, pending order expiration from the original EA is represented by the cooldown mechanic instead of actual order cancellation.
