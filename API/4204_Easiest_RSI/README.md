# Easiest RSI Strategy (ID 4204)

Converted from the MetaTrader 4 expert advisor **"Easiest RSI"** located in `MQL/9827/Easiest_RSI.mq4`.

## Overview

The original EA opens trades when the Relative Strength Index (RSI) crosses out of oversold/overbought zones and optionally adds up to two extra positions in the same direction as price keeps moving favourably. Each order uses the same volume, a fixed stop-loss, and a trailing-stop that advances in small steps once the trade is deep in profit.

This StockSharp port keeps the behaviour at the strategy level:

- RSI(14) calculated on the configured candle series drives the signals.
- Long trades are triggered when RSI crosses upward through the oversold threshold; shorts appear on downward crossings through the overbought threshold.
- Position scaling mimics the MT4 averaging logic by adding a new order every time price advances by `StepPips`, limited by `MaxEntries`.
- Initial stops and trailing stops are managed internally with price distances measured in pips (adjusted automatically for 4/5 digit FX quotes).
- All state (RSI history, last entry prices, trailing stops) is stored in primitive fields to follow the framework guidelines.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `LotSize` | `1` | Volume of each market order. |
| `StopLossPips` | `50` | Initial protective stop in pips (set to zero to disable). |
| `TrailingStopPips` | `50` | Trailing-stop distance in pips; zero disables trailing. |
| `StepPips` | `20` | Minimum favourable move before an additional position is added. |
| `RsiPeriod` | `14` | RSI length. |
| `OversoldLevel` | `30` | RSI level that must be crossed upward to trigger long entries. |
| `OverboughtLevel` | `70` | RSI level that must be crossed downward to trigger short entries. |
| `MaxEntries` | `3` | Maximum number of sequential entries per direction (matching the MT4 limit). |
| `CandleType` | `TimeFrame(5m)` | Candle type/time frame used to compute RSI. |

All distances expressed in pips are converted to absolute prices using the instrument `Step` value. For 5-digit FX symbols the helper doubles the step so that inputs such as `50` equate to 5.0 pips, mirroring the original EA guidance.

## Trading Logic

1. **Signal detection** – The strategy watches finished candles only. It stores the last two RSI readings to replicate the MT4 calls `iRSI(..., 1)` and `iRSI(..., 2)`. Crosses through `OversoldLevel` or `OverboughtLevel` fire once the new candle closes.
2. **Primary entries** – When flat and a bullish cross occurs, a buy market order is sent; bearish crosses when flat trigger a sell order.
3. **Scaling in** – While a position is open, the strategy compares the latest close/high (long) or close/low (short) against the price of the most recent fill. Every time price moves by at least `StepPips` in favour, a new order with size `LotSize` is submitted, up to `MaxEntries` total positions in that direction.
4. **Stop-loss** – On each fill, an initial stop is recalculated as the position price minus/plus `StopLossPips`. The aggregated stop keeps the farthest (most conservative) distance so that the entire position remains protected.
5. **Trailing** – After the trade progresses, the stop is advanced closer using the candle high (longs) or low (shorts). A small buffer equivalent to five minimum price steps emulates the MT4 requirement `OrderStopLoss() + 5*Point` before the stop is moved.
6. **Exit** – When price hits the managed stop level the position is closed at market. No profit target is used beyond the trailing stop.

## Implementation Notes

- Orders are sent through the high-level `SubscribeCandles().Bind(...)` pipeline and market order helpers (`BuyMarket` / `SellMarket`).
- The strategy maintains `_longOrderPending` / `_shortOrderPending` and exit flags to avoid flooding the exchange with duplicate requests while a market order is waiting for confirmation.
- `StartProtection` is not invoked because all protective logic is coded explicitly to match the MT4 behaviour.
- Because StockSharp works with net positions, the trailing stop is applied to the aggregate exposure. This means when multiple entries are open, all lots exit together once the combined stop is touched. The original EA moved each order’s stop individually; the aggregate approach keeps risk control but can close the basket slightly earlier. The difference is documented for transparency.

## Usage Tips

1. Assign the desired security and connector, then set `CandleType` to match the timeframe you want to trade (e.g. 5-minute EURUSD candles as in the source comments).
2. Adjust pip-based parameters according to the instrument’s volatility. Remember to multiply the defaults by 10 if you prefer to work in raw points for 5-digit quotes, mirroring the MT4 guidance.
3. Optional: tweak `MaxEntries` and `StepPips` to manage how aggressively the strategy averages into winning trades.
4. Run the strategy in paper trading first to validate pip conversions and trailing behaviour on your broker’s symbols.

## Files

- `CS/EasiestRsiStrategy.cs` – Strategy implementation.
- `README.md` – This document.
- `README_cn.md` – Chinese translation.
- `README_ru.md` – Russian translation.

Python implementation is intentionally omitted as requested.
