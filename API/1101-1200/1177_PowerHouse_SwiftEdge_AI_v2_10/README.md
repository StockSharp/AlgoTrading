# PowerHouse SwiftEdge AI v2.10 Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Pivot-based breakout strategy with momentum filter and simple risk management.
Long entry occurs when price breaks the recent pivot high with sufficient momentum and minimum distance from previous signal.
Short entry triggers on a break of the pivot low with negative momentum.
Positions use take-profit and stop-loss specified in points.

## Calculation and execution

This is a deterministic example; the historical title does not imply a trained AI model. C# and Python share the following explicit interpretation of the brief above.

Use finished base candles. A pivot high is strictly higher than the highs of `PivotPeriod` neighboring bars on each side; a pivot low is strictly lower than their lows. Equal prices do not form a pivot. Confirmation arrives only after the right-hand bars finish, without backdated signals. Retain the latest confirmed high and low independently.

A long requires `previous Close <= pivot high` and `current Close > pivot high`; a short requires the opposite crossing of the pivot low. Momentum is `current Close - Close[MomentumPeriod bars ago]`. It must exceed `MomentumThreshold` for long or be below its negative for short. Both sides also require at least `MinSignalBars` finished bars since the last accepted entry; zero disables spacing. Rejected signals are not queued. There is no EMA filter or six-hour cooldown.

Enter `Volume` from flat, or fully reverse an opposite position with `Volume + abs(Position)`. A pending order blocks additional orders. Stop/take distances are fixed absolute quote-price units: one point equals a price difference of 1, not a tick, `PriceStep` multiplier or percent. Adjust them for the instrument. The anchor is the actual fill price, volume-weighted for same-side partial additions; reducing a position retains its basis and a reversal starts a new basis.

Local protection closes the entire held quantity by market order when the executable bid for long or ask for short reaches the stop or take. Finished candle closes also check protection; candle high/low touches are not treated as ordered intrabar events. Protection ignores the entry interval and takes priority over a new entry on the same candle. Market fills may differ from the trigger across gaps.

Defaults: `PivotPeriod=2`, `MomentumPeriod=10`, `MomentumThreshold=0`, `MinSignalBars=5`, `StopLossPoints=50`, `TakeProfitPoints=100`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. Periods and both exit distances must be positive; momentum threshold and spacing cannot be negative.
