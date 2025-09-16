# MT45 Strategy

## Overview
The MT45 strategy is a direct conversion of the original MetaTrader expert advisor. It alternates between long and short market positions on every completed bar, while protecting each trade with the same fixed take-profit and stop-loss distances that were used in the MQL implementation. Position sizing follows a martingale-style recovery rule so that the next trade increases its volume only after a losing result.

## Trading Logic
1. The strategy subscribes to a single candle series defined by the **Candle Type** parameter and waits for finished candles to avoid intra-bar noise.
2. When no position is open and the previous entry order has been fully processed, the algorithm submits a market order in the direction scheduled for this turn (buy, then sell, then buy, ...).
3. The direction toggles only after the corresponding order is filled, ensuring that the alternation matches the behaviour of the MQL expert where each completed trade flips the side for the next signal.
4. Protective stop-loss and take-profit orders are managed automatically through `StartProtection`, so the strategy leaves the market when either distance is reached.

## Position Sizing
* **Base Volume** sets the initial lot size. It is restored after every profitable or breakeven trade.
* After a losing trade the volume for the next entry is multiplied by **Martingale Multiplier**. If the scaled value would exceed **Max Volume**, the strategy falls back to the base volume to avoid uncontrolled growth.
* The realised profit or loss is measured by comparing the exit price with the stored entry price, which reproduces the `Lot()` function of the original expert advisor.

## Risk Management
* **Stop Points** and **Take Points** are expressed in price steps, mirroring the `_Point` multiplier that was used on MetaTrader. The strategy converts those values to absolute price distances via the instrument `PriceStep` before enabling `StartProtection`.
* Protective orders are attached automatically to every position and are placed symmetrically for both long and short trades.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| Stop Points | Distance to the protective stop in instrument price steps. | 600 |
| Take Points | Distance to the take-profit target in instrument price steps. | 700 |
| Base Volume | Base volume used for new positions after wins. | 0.01 |
| Martingale Multiplier | Volume multiplier applied after losses. | 2 |
| Max Volume | Maximum allowed volume for martingale scaling. | 10 |
| Candle Type | Candle series used to detect bar completion (default: 1 minute). | 1 minute |

## Usage Notes
* Choose the candle timeframe that matches the chart timeframe of the original expert. The logic operates strictly on completed candles.
* The strategy does not queue another entry while an order is pending or a position is active; it always waits for the existing trade to close through stop-loss or take-profit.
* There is no separate Python version for this strategy at the moment, matching the project guidelines.
