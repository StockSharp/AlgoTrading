# e-TurboFx Momentum Exhaustion Strategy

## Overview
The **e-TurboFx Momentum Exhaustion Strategy** is a direct port of the original MetaTrader 4 expert advisor "e-TurboFx". The system scans the most recent finished candles and looks for directional stretches where the candle bodies keep expanding. Consecutive bearish candles with growing body size signal a potential capitulation that can be faded with a long entry, whereas consecutive bullish candles with expanding bodies hint at an overextended rally that may be sold short. The StockSharp implementation keeps the logic event-driven through candle subscriptions and automatically attaches optional stop-loss and take-profit protection.

## Trading logic
1. Subscribe to a configurable candle type (time frame) and process only finished candles.
2. Track two separate sequences: one for bearish candles and another for bullish candles.
3. For each candle, measure the absolute body size (`|Close - Open|`).
4. Reset the opposite-direction sequence as soon as a candle closes in the other direction.
5. Within each sequence require strictly expanding bodies — every new candle must have a larger body than the previous one. Any contraction restarts the sequence counter from 1.
6. When the number of candles in a sequence reaches `DepthAnalysis`, trigger a market entry in the opposite direction of the last sequence (buy after bearish streaks, sell after bullish streaks).
7. Once a position is open, pause signal detection until the strategy returns to a flat position. Built-in `StartProtection` manages optional stop-loss and take-profit distances expressed in price steps (ticks).

This behaviour reproduces the MQL4 algorithm where the expert adviser checked the last `N` closed candles and confirmed that all bodies were aligned in the same direction and that each body was larger than the body of the next older candle.

## Implementation details
- Uses the high-level candle subscription API with `SubscribeCandles` and `Bind` to stay compliant with the project guidelines.
- Keeps only scalar fields (`_bearishSequence`, `_bullishSequence`, `_previousBearishBody`, `_previousBullishBody`) to avoid custom collections and rely on internal state between events.
- Calls `StartProtection` only once in `OnStarted` to configure optional stop-loss and take-profit orders in price steps. A value of `0` disables each protective order just like the original expert.
- Provides extensive English comments in the source code, including explanations for resets and entry triggers.
- Draws candles and own trades on a chart area when running inside Designer or the UI to ease debugging.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `DepthAnalysis` | Number of consecutive finished candles required in one direction with expanding bodies before opening a trade. | `3` |
| `TakeProfitSteps` | Take-profit distance measured in exchange price steps (ticks). Set to `0` to disable the take profit. | `120` |
| `StopLossSteps` | Stop-loss distance measured in exchange price steps (ticks). Set to `0` to disable the stop loss. | `70` |
| `TradeVolume` | Volume sent with each market order. Changing this parameter also updates the base `Strategy.Volume`. | `0.1` |
| `CandleType` | Candle data type (time frame) subscribed for the analysis. | `1 hour` |

All numeric parameters expose optimization metadata so that the strategy can be tuned in StockSharp optimizers if desired.

## Notes and recommendations
- Because the strategy reacts to candle body expansion, the chosen timeframe significantly affects signal frequency. Shorter intervals produce more trades but may require tighter protective distances.
- Ensure that the connected security defines a valid `PriceStep`; otherwise the step-based protective distances cannot be converted to absolute prices.
- Backtest the port inside the StockSharp Designer before live deployment to validate how the stop and target translate for the selected instrument.
- The strategy keeps a single open position at a time. After every exit the counters are reset and the pattern must rebuild from scratch, mirroring the original MQL4 behaviour.
