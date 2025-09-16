# 20 Pips Opposite Last N Hour Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy is a high-level port of the MetaTrader Expert Advisor
**"20 Pips Opposite Last N Hour Trend"**. It observes hourly candles, gauges
how price behaved during the previous `N` hours, and then opens a position in
the opposite direction when the configured trading hour finishes. The trade is
managed using a fixed 20 pip take-profit target and an hourly time-out, while
a martingale-style volume ladder is applied after consecutive losses.

The implementation uses StockSharp's candle subscriptions, parameter system,
and order helpers (`BuyMarket`, `SellMarket`) so it can run unchanged inside
Designer, API, Runner, or Shell.

## Trading Logic

- The strategy subscribes to the selected candle type (default: 1-hour bars).
- For each finished candle it keeps the close price inside an internal history.
- When a candle with `OpenTime.Hour == TradingHour` is completed and enough
  history is available:
  - Compare the close that happened `HoursToCheckTrend` bars ago with the
    previous close (1 bar ago).
  - If price decreased over that window (bearish drift) the strategy buys;
    if price increased (bullish drift) it sells. Equal closes skip trading.
- Only one trade is opened per day and exclusively on the configured trading
  hour. All other candles are used purely for management.

## Position Management

- A 20-pip target (adjusted for 3/5 digit symbols) is computed right after the
  entry. When any finished candle shows that the high/low touched the target the
  position is closed at that level.
- If the target is not reached during the next hour, the position is closed at
  the end of the following candle to avoid overnight exposure.
- Daily counters are reset automatically when a new trading day starts, so the
  next eligible signal can fire on the following session.

## Money Management

- `Volume` sets the base order size. `MaxVolume` caps the resulting size of any
  martingale step.
- After a losing exit the strategy increases the next position by the
  appropriate multiplier: first loss → `FirstMultiplier`, second loss →
  `SecondMultiplier`, etc. Losing streaks beyond five trades reuse the fifth
  multiplier. Any profitable or break-even close resets the sequence.
- Volume calculations rely on the last executed position price, so profit/loss
  detection remains deterministic even without full broker PnL data.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaxPositions` | 9 | Maximum trades allowed per day. Set to 0 to disable trading. |
| `Volume` | 0.1 | Base volume for the first trade of a streak. |
| `MaxVolume` | 5 | Hard cap for the adjusted volume after multipliers. |
| `TakeProfitPips` | 20 | Take-profit distance in pips. 0 disables the TP. |
| `TradingHour` | 7 | Hour of the day (0-23) that is eligible for opening a position. |
| `HoursToCheckTrend` | 24 | Number of hourly closes used to measure the prior trend. |
| `FirstMultiplier` | 2 | Multiplier applied after the first consecutive loss. |
| `SecondMultiplier` | 4 | Multiplier applied after the second consecutive loss. |
| `ThirdMultiplier` | 8 | Multiplier applied after the third consecutive loss. |
| `FourthMultiplier` | 16 | Multiplier applied after the fourth consecutive loss. |
| `FifthMultiplier` | 32 | Multiplier applied from the fifth loss onward. |
| `CandleType` | H1 | Candle data type used for signal generation and management. |

## Additional Notes

- Pip size is calculated from `Security.PriceStep` and the number of decimals so
  the 20-pip target behaves correctly on both 4- and 5-digit FX symbols.
- `StartProtection()` is invoked when the strategy starts, enabling built-in
  StockSharp protections (auto stop for unbound positions, portfolio resets).
- The logic only uses finished candles and never reads indicator values
  directly, matching the guidelines from `AGENTS.md`.

> **Risk Disclaimer:** Martingale-style position sizing can lead to substantial
> drawdowns. Always test the parameters on historical data and use prudential
> risk limits before deploying to live trading.
