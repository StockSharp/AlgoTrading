# Early Top Prorate V1 Strategy

This document describes the StockSharp port of the MetaTrader expert advisor **earlyTopProrate_V1**. The strategy searches for intraday moves that extend away from the daily open and scales out of the position using three profit targets. It was converted using the high level StockSharp API while preserving the original money management and trade management ideas.

## Core Logic

1. **Daily context** – the strategy reconstructs the current day's open, high and low from the processed candles. The dominant direction is defined by comparing `high - open` and `open - low`.
2. **Entry window** – new trades may only be opened between `StartHour` (inclusive) and `EndHour` (exclusive). The default configuration trades the early European session.
3. **Entry conditions** –
   - When the dominant direction is bullish and the latest closing price is above the daily open the strategy opens a long position.
   - When the dominant direction is bearish and the latest closing price is below the daily open the strategy opens a short position.
   - Only one market position is allowed at the same time (`MaxPositions = 1` by default).
4. **Money management** – the volume of each entry is obtained from the selected money management mode (see below). The value is rounded using the instrument volume step and clamped between the exchange minimum and maximum volume.
5. **Position handling** – after entering a position the strategy applies the layered exit rules listed in the next section. The rules mirror the original expert advisor but are implemented with high level StockSharp orders instead of direct stop-loss/take-profit modifications.
6. **Session close** – if a position remains open when `ClosingHour` is reached the strategy forces an exit at market.

## Trade Management Details

The original MQL expert advisor relies on manual stop and take-profit adjustments. The StockSharp port reproduces the behaviour with explicit checks on each finished candle:

- **Break-even rescue** (`BreakEvenTrigger`) – if price moves against the entry by the configured number of points the strategy waits for a recovery back to the entry price and then exits at break-even.
- **Emergency stop** (`StopLoss0`) – when the adverse excursion exceeds this distance the position is closed immediately.
- **Stop to entry** (`StopLoss1`) – after a positive move of the specified distance the protective stop is moved to the entry price.
- **Stop in profit** (`StopLoss2`) – once the profit reaches this threshold the protective stop is pushed above (long) or below (short) the entry. The offset equals `StopLoss2 - StopLoss1`, reproducing the `setSL2-35` logic from MetaTrader.
- **Scaling out** (`TakeProfit1/2/3` and `Ratio1/2/3`) – three profit objectives trigger partial closures of the remaining position volume. Ratios represent percentages of the current position so that subsequent targets work on the reduced exposure. The third target closes the entire remainder.

All distance based parameters operate in *points*. The helper parameter `PointMultiplier` multiplies the instrument `PriceStep` to reproduce the `value * 10 * Point` arithmetic from the original script (default multiplier = 10).

## Money Management Modes

The parameter `MoneyManagementType` selects one of four sizing models:

| Mode | Description |
| --- | --- |
| `0` or `1` | Fixed lot size equal to `BaseVolume` (mirrors the MQL behaviour where modes 0 and 1 are identical). |
| `2` | Square root model – uses `0.1 * sqrt(balance / 1000) * MoneyManagementFactor`. The current portfolio value is used when available. |
| `3` | Equity risk model – computes `equity / price / 1000 * MoneyManagementRiskPercent / 100`, approximating the `AccountEquity/Close[0]` formula from MetaTrader. |

Each result is normalized using the instrument volume step and the exchange min/max volume.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Candle series used for decisions. Defaults to 5-minute candles. |
| `StartHour` / `EndHour` | Trading window in hours (0–23). |
| `ClosingHour` | Hour when any open position is closed. |
| `TimeZoneShift` | Informational time zone offset kept for compatibility. |
| `BaseVolume` | Base lot size before money management adjustments. |
| `MaxPositions` | Maximum simultaneous positions (default = 1). |
| `TakeProfit1`, `TakeProfit2`, `TakeProfit3` | Distances, in points, of the three profit targets. |
| `BreakEvenTrigger` | Loss, in points, that activates the break-even rescue exit. |
| `StopLoss0`, `StopLoss1`, `StopLoss2` | Adverse/profitable thresholds controlling the protective stop logic. |
| `Ratio1`, `Ratio2`, `Ratio3` | Percentages of the current position closed at each target. |
| `MoneyManagementType` | Money management mode (0–3). |
| `MoneyManagementFactor` | Multiplier for the square root model. |
| `MoneyManagementRiskPercent` | Risk percentage for the equity model. |
| `PointMultiplier` | Multiplier applied to the instrument price step when translating points to actual price offsets. |

## Usage Notes

- Choose a candle type that matches the data granularity available on the selected venue. The default 5-minute series provides a balance between responsiveness and noise filtering.
- When converting point-based distances to real prices the strategy multiplies `PriceStep * PointMultiplier`. Adjust the multiplier if the broker defines points differently from the original MetaTrader environment.
- The break-even and trailing logic requires finished candles, therefore intrabar behaviour may slightly differ from the tick-based MetaTrader execution. The README highlights this approximation so it can be accounted for during testing.
- `TimeZoneShift` is preserved for documentation. Trading hours themselves must be configured using `StartHour`, `EndHour`, and `ClosingHour`.

## Getting Started

1. Add the strategy to your StockSharp project or run it inside Designer/Runner.
2. Configure the candle series (`CandleType`) and trading hours for the instrument you intend to trade.
3. Tune the point-based thresholds and ratios according to the instrument volatility.
4. Select a money management mode and set the corresponding parameters (`BaseVolume`, `MoneyManagementFactor`, `MoneyManagementRiskPercent`).
5. Run the strategy in paper trading first to validate that the behaviour matches your expectations before using it with live capital.

