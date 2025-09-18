# Gselector Pattern Probability Strategy

## Overview
The **Gselector Pattern Probability** strategy is a StockSharp port of the MetaTrader 4 "Gselector" expert. It studies direction changes of synthetic price series built from multiple step sizes, keeps probability statistics for every observed pattern, and trades when the probability of a continuation move is high enough. Stop-loss and take-profit distances are simulated in software to mirror the original expert behaviour.

## Learning Process
1. **Synthetic ladders** – For each configured delta multiple the strategy constructs a step-based series by recording the last closing price every time the market moves by the required distance.
2. **Pattern encoding** – A bit mask is created by comparing every pair of neighbouring values inside the ladder. Rising steps get bit `0`, falling steps get bit `1`, which reproduces the `Ncomb` encoding from the MQL implementation.
3. **Event tracking** – When a new pattern appears, the strategy starts watchers for every configured stop level. A watcher stores the origin price and waits until price moves up or down by the threshold.
4. **Probability update** – Once a watcher completes, upward moves increase the "growth" statistic, downward moves increase the "decline" statistic. A forgetting factor emulates the decay logic (`forg`) of the original expert.
5. **Persistence in memory** – All statistics are kept in memory and reset on strategy start, matching the behaviour of the MQL version when `ReadHistory` is disabled.

## Trading Logic
1. Continuation probabilities are calculated for the current pattern on every delta ladder.
2. A buy signal requires:
   - Probability ≥ `ProbabilityThreshold`.
   - Observations ≥ `MinSamples`.
   - Cooldown elapsed since the previous buy.
   - If a short position exists, the new probability must exceed the stored sell probability plus the `ProbabilityBuffer`.
3. A sell signal mirrors the buy rules with the growth/decline roles swapped.
4. Entries use `BuyMarket` / `SellMarket` to emulate `OrderSend`. When the opposite position is open the strategy closes it first, reproducing the reversal behaviour of the expert adviser.
5. Protective exits are handled internally: stops and takes are expressed in price units derived from the point value and stop level.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle data type used for the backtest/live session. | 1-minute time frame |
| `ProbabilityThreshold` | Minimum continuation probability required to open a trade. | 0.8 |
| `BaseDeltaPoints` | Base point distance that defines the first synthetic ladder. | 1 |
| `DeltaSteps` | Number of delta ladders to evaluate. | 20 |
| `PatternLength` | Number of elements in the ladder history. | 10 |
| `StopLevels` | Count of stop/take levels. | 1 |
| `StopDistancePoints` | Base stop/take distance in points. | 25 |
| `ForgetFactor` | Decay applied to growth/decline counters after each observation. | 1.05 |
| `MinSamples` | Minimum number of completed observations. | 10 |
| `ProbabilityBuffer` | Extra probability required to close the opposite position. | 0.05 |
| `FixedVolume` | Base trade volume. | 1 lot |
| `UseReinvest` | Enables balance-proportional volume adjustment. | true |
| `VolumeMode` | 0 – fixed, 1 – percent per 10k, 2 – ladder, 3 – linear. | 1 |
| `PercentPer10k` | Percentage per 10 000 units in mode 1. | 3 |
| `BaseDeposit` | Base deposit for modes 2 and 3. | 500 |
| `DepositStep` | Deposit increment for modes 2 and 3. | 500 |
| `MaxVolume` | Maximum volume cap. | 10000 |
| `CooldownFactor` | Number of candle intervals used as the reactivation cooldown. | 2 |

## Differences from the MQL Expert
- File-based persistence was removed; statistics are rebuilt from scratch whenever the strategy starts.
- Orders are simulated through `BuyMarket`/`SellMarket` and software stop management instead of MT4 pending orders.
- The position sizing helpers were adapted to StockSharp portfolio data. If equity values are not available the strategy falls back to the fixed volume.
- Trailing stop inputs from the original code are ignored because the MT4 version never applied them.

## Usage Notes
- Attach the strategy to a security with a valid `PriceStep`. If the step is unknown the strategy falls back to 0.0001.
- The learning process needs a minimum number of ladder activations; expect a warm-up phase before trades start.
- Increasing `DeltaSteps` or `PatternLength` raises memory usage exponentially because the pattern dictionary grows quickly.
- The default probability threshold (0.8) is very strict. Lower the value for more frequent trades.
