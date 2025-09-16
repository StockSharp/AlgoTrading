# Six Indicators Momentum Strategy

This strategy reproduces the MetaTrader 4 expert advisor **6xIndics_M** using the StockSharp high-level API. It mixes six momentum inputs derived from Bill Williams' Accelerator Oscillator (AC) and Awesome Oscillator (AO) and feeds them through a selectable decision matrix. A slow stochastic oscillator acts as the final filter. Only one position is open at a time; martingale money management, stop-loss/take-profit and optional trailing stops emulate the original behaviour.

## How the strategy works

1. **Data subscription** – the strategy subscribes to the configured candle series (`CandleType`, default 1-hour bars).
2. **Indicators**
   - Awesome Oscillator calculates the difference between the 5- and 34-period simple moving averages of the median price.
   - A 5-period simple moving average of the AO produces the Accelerator Oscillator values (AC).
   - A Stochastic Oscillator with parameters 5/5/5 supplies the %K line that is delayed by one closed candle (MT4 shift = 1).
3. **Six indicator slots** – every finished candle fills the following buffers:
   - Slot 0: AC value shifted by 1 candle (`AC[1]`).
   - Slot 1: AC value shifted by 10 candles (`AC[10]`).
   - Slot 2: AC value shifted by 20 candles (`AC[20]`).
   - Slot 3: AO momentum, i.e. `AO[0] - AO[shift]`, where the shift is configurable (`AoMomentumShift`).
   - Slot 4: AC momentum `AC[0] - AC[shift #1]` (`AcPrimaryShift`).
   - Slot 5: AC momentum `AC[0] - AC[shift #2]` (`AcSecondaryShift`).
4. **Selectable signal matrix** – parameters `FirstSourceIndex` … `SixthSourceIndex` pick which slot feeds the six Boolean checks originally named `k`, `u`, `t`, `e`, `r`, `o`. The same indices are reused both for generating entries and for closing trades when `CloseOnReverseSignal` is enabled.
5. **Entry logic**
   - **Buy** when the chosen slots satisfy: `A > 0`, `B > 0.0001 × Sensitivity`, `C > 0.0002 × Sensitivity`, `D < 0`, `E < 0.0001 × Sensitivity`, `F < 0.0002 × Sensitivity`, and the previous stochastic %K is below 15.
   - **Sell** when `A < 0`, `B < 0.0001 × Sensitivity`, `C < 0.0002 × Sensitivity`, `D > 0`, `E > 0.0001 × Sensitivity`, `F > 0.0002 × Sensitivity`, and the previous stochastic %K is above 85.
6. **Position management**
   - Only one position is allowed. When a trade is open the strategy skips new entries, mirroring the MT4 expert.
   - Stop-loss and take-profit levels are converted from pips into absolute prices using the instrument tick size (exactly as `Point` works in MT4).
   - Optional trailing stop replicates the original behaviour: it activates once price moves by `TrailingStopPips` beyond the entry (and, when `RequireProfitForTrailing` is true, by an extra `LockProfitPips`). The stop follows price only in the favourable direction.
   - `CloseOnReverseSignal` closes a profitable trade if the opposite signal appears (Bid above the entry for longs, Ask below for shorts).
7. **Martingale sizing** – when enabled, the next order volume equals the previous trade volume multiplied by `(TakeProfitPips + StopLossPips) / TakeProfitPips` whenever a trade closes at a loss or break-even. Winning trades reset the size to the base `Volume`.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `AllowBuy`, `AllowSell` | Enable or disable long/short entries. | `true` |
| `CloseOnReverseSignal` | Close the current position when an opposite signal appears while the trade is in profit. | `false` |
| `FirstSourceIndex` … `SixthSourceIndex` | Choose which of the six indicator slots feeds each logical check. Values outside 0–5 are clamped. | `1,2,3,4,3,4` |
| `AoMomentumShift` | Number of bars between the current AO value and the comparison used in slot 3. | `10` |
| `AcPrimaryShift`, `AcSecondaryShift` | Number of bars between the current AC value and the comparisons for slots 4 and 5. | `10` / `10` |
| `SensitivityMultiplier` | Multiplier applied to the 0.0001 and 0.0002 thresholds used in the slot checks. | `1.0` |
| `TakeProfitPips`, `StopLossPips` | Exit distances expressed in MetaTrader-style pips (they are rescaled by the tick size). | `300` / `300` |
| `UseTrailingStop` | Enable the trailing stop logic. | `false` |
| `TrailingStopPips` | Distance between price and trailing stop, in pips. | `300` |
| `RequireProfitForTrailing` | When enabled, the trailing stop activates only after the trade gains an extra `LockProfitPips`. | `false` |
| `LockProfitPips` | Additional profit (in pips) that must be locked before the trailing stop starts moving. | `300` |
| `Volume` | Base order size. | `0.1` |
| `UseMartingale` | Enable martingale position sizing. | `false` |
| `CandleType` | Candle series used for all calculations. | `TimeSpan.FromHours(1)` |

## Notes and best practices

- Every candle is processed only after it finishes, so signals mimic the MT4 expert that executed once per bar (`prevtime` guard in the original code).
- The strategy stores only the required history (up to 256 bars) to reproduce the MT4 shift calculations without calling `GetValue()` on indicators, satisfying the project guidelines.
- Trailing and stop/limit exits are simulated on candle highs/lows. In a live environment you should use real stop orders for guaranteed execution.
- Martingale sizing uses the instrument’s `VolumeStep`, `MinVolume`, and `MaxVolume` limits to keep volumes within broker rules.
- When `AllowBuy` or `AllowSell` is disabled, the corresponding signals are ignored, but the opposite signal can still be used for `CloseOnReverseSignal`.

## Differences versus the MT4 expert

- Indicator calculations use StockSharp’s built-in Awesome Oscillator and SMA classes; no manual buffer management is required.
- All trades are executed via market orders (`BuyMarket` / `SellMarket`) and exits via `ClosePosition()`, while the MT4 version sent explicit `OrderSend`/`OrderClose` requests.
- Lot sizing respects the exchange volume granularity by rounding to `VolumeStep` and clamping to `[MinVolume, MaxVolume]`.
- Chart helpers (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) are added for visual inspection when a chart is available.
