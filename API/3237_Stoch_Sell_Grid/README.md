# Stoch Sell Strategy

## Overview
This strategy reproduces the behaviour of the original **stochSell** MetaTrader expert. It listens to a single candle stream and waits for a triple stochastic confirmation combined with a volatility filter before sending an initial market sell order. Immediately after the short entry it deploys a ladder of pending sell stops to scale into the move if price keeps falling.

## Trading Logic
- **Volatility filter** – an Average True Range (ATR) with configurable length must stay below the specified threshold.
- **Slow stochastic confirmation** – the longest stochastic oscillator must stay below the long-term oversold level before any trades are allowed.
- **Cross confirmation** – both the medium and the fast stochastic oscillators must cross down through the oversold trigger during the same finished candle.
- **Position check** – new entries are placed only when the strategy has no active orders and the position is flat.

Once all conditions are met the strategy sends a market sell order using the configured volume and immediately schedules a set of sell stop orders according to the grid settings. Pending orders are optional and can be disabled by setting the grid order count to zero.

## Exit Rules
- **Profit target** – when the short basket accumulates the desired profit in pips (calculated from the volume-weighted entry price), the strategy buys back the entire position and removes every remaining pending order.
- **Manual stop** – grid orders respect a configurable lifetime. When a stop order expires without being filled it is cancelled automatically.
- **Full close** – any buy trade that returns the position to zero clears the internal entry statistics and cancels the pending grid.

## Grid Management
- Pending orders are placed below the reference price using the start offset and step expressed in pips.
- Each pending order uses the grid volume multiplier, allowing the basket size to differ from the initial market entry.
- Expiration (in minutes) is applied to every pending order; zero disables the timeout.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Primary timeframe for every indicator and trading decision. |
| `AtrPeriod` / `AtrThreshold` | Volatility filter controlling when the strategy is allowed to trade. |
| `FastKPeriod`, `FastDPeriod`, `FastSlowing` | Configuration of the fast stochastic oscillator. |
| `MediumKPeriod`, `MediumDPeriod`, `MediumSlowing` | Configuration of the medium stochastic oscillator. |
| `SlowKPeriod`, `SlowDPeriod`, `SlowSlowing` | Configuration of the slow stochastic oscillator. |
| `OversoldLevel` | Level that the fast and medium stochastic values must cross downward. |
| `LongTermOversoldLevel` | Upper bound for the slow stochastic during entry. |
| `ProfitTargetPips` | Net profit in pips required to close the short basket. |
| `GridOrdersCount` | Number of pending sell stops created after the entry. |
| `GridStartOffsetPips` | Offset in pips between the entry price and the first pending order. |
| `GridStepPips` | Distance in pips between consecutive pending orders. |
| `GridVolume` | Volume applied to each pending order. |
| `GridExpirationMinutes` | Lifetime of pending orders in minutes. |
| `MarketVolume` | Volume used for the initial market sell. |

## Notes
- Indicator values are processed through the high-level `BindEx` API and only finished candles trigger trading decisions.
- The position tracking logic keeps a volume-weighted entry price in order to translate the raw profit target into pips.
- To disable scaling simply set the grid order count to zero; the strategy will still rely on the stochastic confirmation and ATR filter for single-shot trades.
