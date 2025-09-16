# Lock Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Lock Strategy recreates the classic “lock” expert advisor from MetaTrader: it always maintains a hedged pair of long and short positions and keeps recycling them until a profit-lock condition is satisfied. The algorithm is designed for instruments with small tick sizes where a fixed pip-based take profit can be applied.

## Trading Workflow

1. **Initial Hedge** – as soon as market data becomes available the strategy opens a long and a short position with the same volume. If both orders are filled the volume used for the next hedge is multiplied by the `LotExponential` factor.
2. **Take Profit Management** – every leg stores its entry price. When the candle close moves by `TakeProfitPips` (converted to instrument ticks) from the entry, the leg is closed with a market order. The opposite side remains open, preserving the hedge-like behaviour from the MQL version.
3. **Re-Hedging** – whenever the total number of active legs is one or zero, the strategy immediately opens a fresh pair. If there are no open legs the base volume resets to `LotSize` before the new pair is created.
4. **Volume Control** – the helper method `AdjustVolume` enforces exchange restrictions: it rounds volumes to the security’s `VolumeStep`, clamps them by `MinVolume` and `MaxVolume`, and cancels the scale-up if the adjusted value becomes zero.

## Profit Lock Condition

The original MQL logic monitors account balance versus equity: when balance exceeds equity by `ExcessBalanceOverEquity` and the equity is at least `MinProfit` above the last locked balance, every leg is closed. The C# implementation mirrors this behaviour by tracking the equity observed when the strategy is flat and treating it as the running balance. Once the condition is triggered all legs are liquidated and the baseline balance is updated before the cycle restarts with `LotSize`.

## Parameters

- `LotSize` – base volume for the first hedge cycle (default: `0.1m`).
- `TakeProfitPips` – pip distance for closing each leg (default: `100`). A value of `0` disables the automatic exit.
- `LotExponential` – multiplier applied to the current volume after both legs open successfully (default: `2m`).
- `ExcessBalanceOverEquity` – tolerated gap between balance and equity before profits are secured (default: `3000m`).
- `MinProfit` – additional equity growth that must be achieved before closing all legs (default: `500m`).
- `CandleType` – timeframe driving the strategy logic (default: 1 minute time frame).

## Implementation Notes

- Pip size is recalculated from `Security.PriceStep` and `Security.Decimals` so the strategy adapts to 3/5-digit FX symbols as well as standard futures or stocks.
- Orders are placed using market execution, mirroring the behaviour of the MQL expert that sends market orders with broker-side take profits.
- The strategy keeps a full history of hedged legs, which enables multiple stacked positions on each side exactly as the source script allowed.
