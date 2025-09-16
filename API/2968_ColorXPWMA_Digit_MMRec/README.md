# ColorXPWMA Digit MMRec Strategy

## Overview
The **ColorXPWMA Digit MMRec Strategy** replicates the MetaTrader expert adviser `Exp_ColorXPWMA_Digit_MMRec`. It uses the ColorXPWMA Digit indicator to identify trend inflection points and wraps the original money-management recounter logic. The indicator builds a power-weighted moving average (PWMA) that is optionally smoothed by a selected moving-average method. The slope of the smoothed line is converted into discrete colors: `2` for an up-slope, `0` for a down-slope and `1` when the direction is flat.

Trading decisions are taken after the indicator colors are evaluated on a configurable historical bar (`SignalBar`). When the previous color (`SignalBar + 1`) was bullish (2) but the bar at `SignalBar` no longer keeps the bullish color, the strategy closes short positions and optionally opens a new long position. The inverse logic is applied when the historical color was bearish (0) but the more recent bar no longer keeps that bearish color.

## Indicator logic
- **Power-weighted moving average** – each bar receives a weight `(period - index)^power`. Larger powers emphasize the latest samples.
- **Smoothing** – the weighted series is passed through a smoothing moving average. Supported methods include SMA, EMA, SMMA, LWMA, Jurik, T3 and Kaufman AMA. JurX, Parabolic and VIDYA options are approximated with exponential smoothing because StockSharp does not expose direct implementations.
- **Color encoding** – the sign of the smoothed slope defines the color buffer that triggers entries and exits.
- **Digit rounding** – the final value can be rounded to a fixed number of digits to match the original "Digit" behaviour.

## Trading rules
1. **Bullish continuation failure**
   - Condition: color at `SignalBar + 1` equals `2` (bullish) and color at `SignalBar` is different from `2`.
   - Action: close active shorts; if long entries are allowed, open a new long position sized by the money-management recounter.
2. **Bearish continuation failure**
   - Condition: color at `SignalBar + 1` equals `0` (bearish) and color at `SignalBar` is different from `0`.
   - Action: close active longs; if short entries are allowed, open a new short position sized by the recounter.

Orders are always executed on the candle close that produced the signal. When switching direction the strategy closes the opposite exposure and immediately opens the new position in a single market order.

## Money management recounter
The strategy keeps a rolling history of closed trade results for longs and shorts. Before opening a new trade it inspects the most recent `BuyTotalTrigger` or `SellTotalTrigger` results:

- If the number of losing trades in that window reaches the respective loss trigger (`BuyLossTrigger` or `SellLossTrigger`), the position size is reduced to `ReducedVolume`.
- Otherwise the standard `NormalVolume` is used.

This reproduces the behaviour of the original `BuyTradeMMRecounterS` and `SellTradeMMRecounterS` routines.

## Parameters
| Group | Parameter | Description |
| --- | --- | --- |
| General | `CandleType` | Timeframe used for both indicator calculations and trading decisions. |
| Indicator | `IndicatorPeriod` | Period of the power-weighted moving average. |
| Indicator | `IndicatorPower` | Exponent applied to weights. Higher values emphasize the latest bars. |
| Indicator | `SmoothingMethod` | Moving-average method used for smoothing. JurX, ParMa and Vidya fall back to an exponential average. |
| Indicator | `SmoothingLength` | Length of the smoothing moving average. |
| Indicator | `SmoothingPhase` | Phase parameter forwarded to smoothers that support it. |
| Indicator | `AppliedPrice` | Source price used by the indicator (close, open, high, low, etc.). |
| Indicator | `RoundingDigits` | Number of decimal digits used to round the indicator output. |
| Logic | `SignalBar` | Historical shift (in bars) used when reading the color buffer. |
| Permissions | `EnableBuyEntries` / `EnableSellEntries` | Allow opening long/short positions. |
| Permissions | `EnableBuyExits` / `EnableSellExits` | Allow closing longs/shorts. |
| Money Management | `NormalVolume` | Default order size. |
| Money Management | `ReducedVolume` | Order size applied after a loss streak. |
| Money Management | `BuyTotalTrigger`, `BuyLossTrigger` | Number of recent long trades to inspect and loss threshold for switching to reduced volume. |
| Money Management | `SellTotalTrigger`, `SellLossTrigger` | Same logic for short trades. |
| Risk Management | `StopLossPoints`, `TakeProfitPoints` | Optional protective distances (points) applied through `StartProtection` if non-zero. |

## Practical notes
- Keep `SignalBar = 1` to mimic the default Expert Advisor behaviour and guarantee that signals are evaluated on fully completed candles.
- The strategy stores only the most recent results needed for the recounter, preventing uncontrolled memory growth.
- Because StockSharp executes orders asynchronously, the strategy assumes fills at the candle close price when updating the loss counters. This mirrors how the original MQL expert worked with historical data.
- JurX, ParMa and Vidya smoothing options are approximations that use exponential smoothing under the hood. If you require the original proprietary filters, implement custom indicator classes and plug them into the strategy.

