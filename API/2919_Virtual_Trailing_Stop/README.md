# Virtual Trailing Stop Strategy

## Overview
The **Virtual Trailing Stop Strategy** is a direct conversion of the MetaTrader expert advisor `Virtual Trailing Stop.mq5` (MQL ID 21362). The original expert only manages protective stops for positions that were opened elsewhere. This C# port reproduces the same behaviour on top of the StockSharp high-level API: it watches the best bid/ask quotes and closes the current position when stop-loss, take-profit or trailing-stop conditions are met.

Unlike entry-driven strategies, this implementation never opens new positions on its own. It is intended to be combined with other automated entries or manual trading sessions when you need to enforce a MetaTrader-style “virtual” trailing stop inside StockSharp.

## Trading logic
1. **Level1 feed** – the strategy subscribes to level1 data and continuously stores the latest bid/ask values.
2. **Pip conversion** – user inputs are defined in *pips*. The strategy converts them to price offsets by multiplying the value by the security `PriceStep`. For 3- and 5-digit forex quotes a 10x multiplier is applied to match MetaTrader’s pip definition.
3. **Stop-loss check** – if a long bid goes below `EntryPrice − StopLoss`, or a short ask rises above `EntryPrice + StopLoss`, the position is closed at market.
4. **Take-profit check** – if a long bid rises above `EntryPrice + TakeProfit`, or a short ask falls below `EntryPrice − TakeProfit`, the position is closed.
5. **Trailing activation** – once price moves by `TrailingStart` pips in favour of the position, a trailing level is created at `Bid − TrailingStop` (long) or `Ask + TrailingStop` (short).
6. **Trailing updates** – each time the unrealised profit increases by at least `TrailingStep` pips, the trailing level is shifted accordingly. Setting the step to zero makes the trail follow every favourable tick.
7. **Trailing exit** – the position is closed when price touches the trailing level while the trade remains profitable (mirroring the `Profit()>0` safeguard from the source EA).

No pending orders are placed. Every exit is executed through market orders to mimic the “virtual” nature of the MQL implementation.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `StopLossPips` | Stop-loss distance in pips. Set to `0` to disable hard stop-loss management. | `0` |
| `TakeProfitPips` | Take-profit distance in pips. Set to `0` to disable take-profit management. | `0` |
| `TrailingStopPips` | Distance between current price and trailing level, measured in pips. | `5` |
| `TrailingStartPips` | Profit threshold (in pips) that must be reached before trailing is activated. | `5` |
| `TrailingStepPips` | Minimum pip increase required before the trailing level is moved again. Use `0` for continuous trailing. | `1` |

All parameters support optimisation thanks to StockSharp `StrategyParam` helpers.

## Implementation notes
- The strategy uses only level1 data (`DataType.Level1`) and does not register chart objects because StockSharp handles visualisation differently from MetaTrader.
- Price conversions rely on `Security.PriceStep` and `Security.Decimals`. If the exchange does not provide this metadata, the fallback pip size is `1`.
- Protection is symmetrical for long and short positions. Trailing values are stored separately for both directions.
- Automatic position seeding that was present in tester mode inside the original EA has been intentionally omitted because StockSharp strategies operate on net positions.

## Usage tips
- Attach the strategy to a portfolio/security pair that already has open positions or is expected to receive them from another component.
- Combine it with discretionary trading or automated entry strategies to emulate MetaTrader-like trade management in StockSharp Designer, Shell, or Runner.
- When trading non-forex instruments, adjust the pip-based inputs to match the instrument’s tick size. Setting `TrailingStopPips = 1` effectively trails by one `PriceStep`.

## Files
- `CS/VirtualTrailingStopStrategy.cs` – strategy implementation.
- `README.md`, `README_cn.md`, `README_ru.md` – multilingual documentation for the strategy.
