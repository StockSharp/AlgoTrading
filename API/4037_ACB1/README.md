# ACB1 Strategy

## Overview

The **ACB1 Strategy** is the StockSharp port of the MetaTrader expert advisor distributed as `MQL/8586/ACB1.MQ4`. The original system trades the EURUSD pair and waits for strong daily breakouts before entering the market. This conversion reproduces the same decision process with StockSharp high level primitives:

- Daily candles (`SignalCandleType`) define the breakout direction and provide the stop and take-profit anchors.
- H4 candles (`TrailCandleType`) determine the trailing distance that is multiplied by `TrailFactor`.
- Orders are executed at market once the breakout conditions are satisfied and the strategy keeps only one net position, mirroring the `OrdersTotal()` checks in the MQL code.
- Stop-loss and take-profit are managed internally: the strategy watches best bid/ask prices and closes the position with market orders when the virtual protective levels are breached.

## Trading rules

1. **Long setup**
   - Use the previous finished daily candle.
   - If `Close > (High + Low) / 2` *and* the current ask price is above the previous high, open a long market position.
   - Stop-loss is placed at the previous low (rounded to the instrument price step).
   - Take-profit equals the entry price plus `(High − Low) × TakeFactor`.

2. **Short setup**
   - If `Close < (High + Low) / 2` *and* the current bid price is below the previous low, open a short market position.
   - Stop-loss is set to the previous high; take-profit subtracts `(High − Low) × TakeFactor` from the entry price.

3. **Trailing stop**
   - The most recent finished `TrailCandleType` candle supplies `(High − Low) × TrailFactor`.
   - For long positions the stop follows `Bid − TrailDistance` while price remains below the take-profit minus the broker stop level.
   - For short positions the stop follows `Ask + TrailDistance` while price stays above the take-profit plus the broker stop level.

4. **Risk guard**
   - The strategy tracks the maximum observed portfolio equity. Trading halts whenever the current equity drops below 50 % of that peak, exactly as in the original advisor.
   - A five second cooldown (`CooldownSeconds`) prevents new orders or stop updates too frequently, reproducing the `TimeLocal()` throttle from MQL.

## Position sizing & risk control

- The volume per trade is derived from `Portfolio.CurrentValue × RiskFraction`.
- Monetary risk per contract is calculated from the stop distance and the security metadata (`PriceStep` and `StepPrice`).
- The resulting size is aligned to `Security.VolumeStep` and clamped to `[Security.MinVolume, Security.MaxVolume]`, then limited by the `MaxVolume` parameter (default 5 lots).
- Orders are skipped when the normalised volume is zero or when the stop distance violates `MinStopDistancePoints`, which emulates the MetaTrader `MODE_STOPLEVEL` check.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `SignalCandleType` | Daily | Candle type used for breakout detection. |
| `TrailCandleType` | 4 hours | Candle type that supplies the trailing stop distance. |
| `TakeFactor` | 0.8 | Multiplier applied to the daily range to compute take-profit. |
| `TrailFactor` | 10 | Multiplier applied to the trailing range when updating the stop. |
| `RiskFraction` | 0.05 | Fraction of portfolio equity risked on each trade (5 %). |
| `MaxVolume` | 5 | Hard cap for the final order volume. |
| `MinStopDistancePoints` | 0 | Minimal stop/take distance expressed in price points; set it to the broker `MODE_STOPLEVEL`. |
| `CooldownSeconds` | 5 | Minimum delay between consecutive trade actions. |

## Implementation notes

- The strategy requires proper instrument metadata: `Security.PriceStep`, `Security.StepPrice`, `Security.VolumeStep`, `Security.MinVolume`, and (if available) `Security.MaxVolume`.
- Protective levels are virtual. StockSharp closes positions via market orders when bid/ask touches the computed stop-loss or take-profit.
- Equity tracking uses `Portfolio.CurrentValue`. If the connector does not provide this field the risk guard will keep trading disabled until it is available.
- Only a single net position is maintained. Opposite signals while a trade is active are ignored until the position is fully closed.
- No Python port is included; this directory only contains the C# implementation and documentation.
