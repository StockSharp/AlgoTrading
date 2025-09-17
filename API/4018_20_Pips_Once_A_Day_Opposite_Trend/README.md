# TwentyPipsOnceADayStrategy

Port of the MetaTrader expert **20pipsOnceADayOppositeLastNHourTrend** implemented with the StockSharp high-level API. The strategy trades once per configured hour and opens a contrarian position against the drift of the last `N` hourly candles. Position size follows a martingale ladder that increases the lot only when a recent trade ended with a loss. The implementation also enforces a daily trading schedule, optional trailing protection, and a maximum holding period.

## Trading Logic

1. The strategy subscribes to hourly candles (configurable through `CandleType`).
2. When a candle closes and the next hour matches `TradingHour`, the strategy evaluates the direction:
   - Compare the closing price of the last completed hour with the close `HoursToCheckTrend` hours ago.
   - If the market fell over that interval, open a long position (fade the bearish drift).
   - If the market rose, open a short position.
3. Only one position can be active at a time (controlled by `MaxOrders`).
4. Each trade inherits a fixed take profit and optional stop-loss/trailing stop, both expressed in pips relative to the instrument's pip size.
5. If the position remains open longer than `OrderMaxAgeSeconds` or the next hour is outside the allowed session defined by `TradingDayHours`, the strategy forcefully closes the trade.

## Money Management

- `FixedVolume` defines the base lot. Set it to `0` to derive the lot from the portfolio value using `RiskPercent`. The risk-based sizing mirrors the original EA logic: `(portfolio value * RiskPercent) / 1000`.
- After the base lot is calculated it is clamped by both the instrument's `VolumeMin/VolumeMax/VolumeStep` and the user-defined `MinVolume` / `MaxVolume` bounds.
- A martingale ladder increases the next lot only if the respective historical trade closed at a loss:
  - `FirstMultiplier` applies when the most recent trade lost.
  - `SecondMultiplier` applies when the latest trade won but the previous one lost.
  - The chain continues up to `FifthMultiplier`, matching the original five-step escalation.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `FixedVolume` | Fixed trading volume. Use `0` to enable risk-based sizing. |
| `MinVolume` / `MaxVolume` | Lower and upper bounds applied after sizing. |
| `RiskPercent` | Portfolio percentage converted into volume when `FixedVolume` equals zero. |
| `MaxOrders` | Maximum number of simultaneously open positions (default `1`). |
| `TradingHour` | Hour of the day (0-23) when new trades may start. |
| `TradingDayHours` | Comma-separated hours or ranges (e.g. `0-7,13-22`) that remain eligible for open positions. When the next hour is outside this set, the strategy exits. |
| `HoursToCheckTrend` | Lookback in hourly candles used for the contrarian comparison. |
| `OrderMaxAgeSeconds` | Maximum holding time in seconds before forcing an exit. |
| `FirstMultiplier` … `FifthMultiplier` | Martingale multipliers assigned to losses found in the last five closed trades. |
| `StopLossPips` | Initial stop loss distance in pips. Set to `0` to disable. |
| `TrailingStopPips` | Trailing stop distance in pips. Set to `0` to disable. |
| `TakeProfitPips` | Take profit distance in pips. |
| `CandleType` | Candle type used for signal generation (defaults to 1-hour time frame). |

## Risk Controls and Exits

- **Take profit / stop loss**: Configured through `TakeProfitPips` and `StopLossPips` with automatic conversion to instrument price units.
- **Trailing stop**: If enabled, the stop is trailed once the trade gains more than the configured number of pips.
- **Time-out exit**: Positions older than `OrderMaxAgeSeconds` are closed at the current candle close price.
- **Session filter**: Positions are closed when the upcoming hour is not included in `TradingDayHours`.

## Usage Notes

- The strategy works with any instrument that provides hourly candles and a valid `PriceStep`. When the instrument uses fractional pips (3 or 5 decimals), the pip size is automatically adjusted.
- To replicate the MetaTrader behaviour, run the strategy on a single instrument with `CandleType` set to an hourly timeframe and keep the default `TradingDayHours` (0-23) to allow trading throughout the day.
- The martingale ladder assumes at most five relevant historical trades. Resetting the strategy clears this history.
- Because the strategy trades at the open of the configured hour using closed candle data, fills occur at the price available when the new hour starts.

## Files

- `CS/TwentyPipsOnceADayStrategy.cs` – main C# implementation.
- `README.md` – English documentation (this file).
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.

Python ports are intentionally omitted for this conversion.
