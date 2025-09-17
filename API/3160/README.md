# Cidomo Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout system converted from the MetaTrader 5 expert advisor "Cidomo". The strategy waits for a new candle on the configured timeframe, measures the recent trading range, and places paired stop orders above and below that range. It manages risk with classic stop-loss/take-profit levels, an optional trailing stop, and two money-management modes (fixed volume or percentage risk).

## How it works

1. On every finished candle of `CandleType`, collect the last `BarsCount` highs and lows to define the short-term channel.
2. Place a buy stop at `highest + IndentPips` and a sell stop at `lowest - IndentPips` (both values expressed in pips and converted to absolute prices).
3. When a stop order is triggered, the opposite pending order is cancelled immediately.
4. For an open position the strategy keeps track of:
   - Initial stop-loss (`StopLossPips`) and take-profit (`TakeProfitPips`).
   - A stepped trailing stop (`TrailingStopPips` / `TrailingStepPips`). The stop is moved only after price advances by at least `TrailingStop + TrailingStep`, mirroring the original EA.
   - Market exits are used to emulate MetaTrader's `PositionModify` calls when the stop or take-profit is touched.
5. When `UseTimeFilter` is enabled, new orders are submitted only within ±30 seconds of `StartHour:StartMinute` (server time), replicating the tight trading window of the source script.

## Money management

- **FixedVolume**: always trades the exact `TradeVolume` specified by the user.
- **RiskPercent**: calculates the order size so that a losing trade at the configured stop-loss distance reduces equity by `RiskPercent`. Volumes are rounded to the instrument's `VolumeStep` and clamped between `MinVolume` / `MaxVolume`.

## Risk controls

- Initial stop-loss and take-profit levels are stored locally and executed via market orders when price crosses the target during the next candle.
- The trailing stop only moves in one direction and respects the step distance from the original EA, preventing constant small adjustments.
- If no stop-loss is configured the risk-based position sizing automatically falls back to the fixed `TradeVolume`.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H4` | Timeframe used to build the breakout range. |
| `BarsCount` | `int` | `15` | Number of completed candles considered when calculating the highest high and lowest low. |
| `IndentPips` | `decimal` | `3` | Offset (in pips) added above/below the range before submitting stop orders. |
| `StopLossPips` | `decimal` | `50` | Protective stop distance in pips. A value of `0` disables the stop. |
| `TakeProfitPips` | `decimal` | `50` | Profit target distance in pips. A value of `0` disables the target. |
| `TrailingStopPips` | `decimal` | `35` | Trailing stop distance in pips. Set to `0` to disable trailing. |
| `TrailingStepPips` | `decimal` | `5` | Minimum extra profit required before tightening the trailing stop. |
| `MoneyManagement` | `CidomoMoneyManagementMode` | `RiskPercent` | Chooses between fixed position size and risk-based sizing. |
| `RiskPercent` | `decimal` | `1` | Percentage of equity risked per trade when `MoneyManagement = RiskPercent`. |
| `TradeVolume` | `decimal` | `0.1` | Fixed order volume used in `FixedVolume` mode or when risk-based sizing cannot be computed. |
| `UseTimeFilter` | `bool` | `false` | Enables the ±30 second time window filter. |
| `StartHour` | `int` | `9` | Hour (0-23) of the trading window centre. |
| `StartMinute` | `int` | `58` | Minute (0-59) of the trading window centre. |

## Notes

- All pip-based parameters automatically adapt to 3- or 5-digit quotes by multiplying the instrument's `PriceStep` by 10, exactly like the MetaTrader implementation.
- Because StockSharp manages stops client-side in this port, ensure the strategy remains connected so that market exits can be issued when protective levels are breached.
