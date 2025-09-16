# V1N1 Lonny Breakout Strategy

## Overview
The V1N1 Lonny Breakout strategy replicates the MetaTrader "V1N1 LONNY" expert advisor. It targets breakouts that emerge around the London and New York sessions by building an opening range and waiting for a decisive close outside that range. The strategy relies on an exponential moving average to capture the prevailing trend and on a stochastic oscillator to filter out overbought or oversold conditions before entering the market.

A configurable risk model allows position sizing by fixed volume or as a percentage of account equity. The implementation also includes optional spread filtering, trailing stops, and a bar-based timeout that closes the trade if momentum fades after a predefined number of candles.

## Trading Logic
1. **Session alignment** – Trading is only allowed between the configured start and end times. The timetable can be shifted according to daylight-saving schedules for either London or New York.
2. **Opening range** – Immediately before the session begins, the strategy records the highs and lows of a fixed number of candles. This range provides the breakout levels used during the trading window.
3. **Trend confirmation** – The exponential moving average (EMA) slope must agree with the trade direction. A bullish breakout requires the EMA to be rising, while a bearish breakout requires it to be falling.
4. **Momentum filter** – The stochastic oscillator must stay inside a configurable zone around the midpoint to avoid entering when the market is already overbought or oversold.
5. **Breakout validation** – The previous candle must close beyond the range high or low by at least the minimum breakout distance but not farther than the maximum distance.
6. **Risk controls** – Each position defines a stop loss from the range boundary and a take-profit target based on a factor of that stop distance. A trailing stop can tighten the exit as the trade progresses, and positions can be forcibly closed after a certain number of candles.

## Parameters
| Name | Description |
| --- | --- |
| `StartTrade` | Session start time. |
| `EndTrade` | Session end time. |
| `SwitchDst` | Daylight-saving handling: Europe (no shift), USA (relative shift between London and New York), or disabled. |
| `RiskMode` | Position sizing mode (percentage of equity or fixed volume). |
| `PositionRisk` | Risk percentage or fixed volume, depending on the mode. |
| `TradeRange` | Number of candles used to build the opening range. |
| `MinRangePoints` / `MaxRangePoints` | Minimum and maximum size of the opening range, in price points. |
| `MinBreakRange` / `MaxBreakRange` | Minimum and maximum acceptable breakout distance above or below the range, in price points. |
| `StopLossPoints` | Stop-loss distance measured from the opposite side of the range, in price points. |
| `TpFactor` | Take-profit multiplier applied to the stop-loss distance. |
| `TrailStopPoints` | Optional trailing stop distance, in price points. Set to zero to disable trailing. |
| `TrendPeriod` | Period for the EMA slope filter. |
| `OverPeriod` | Period for the stochastic oscillator. |
| `OverLevels` | Distance from 50 used to define the acceptable stochastic range. |
| `BarsToClose` | Maximum number of candles to keep the position open. Zero disables the timeout. |
| `MaxSpreadPoints` | Maximum allowed spread in price points. |
| `SlippagePoints` | Reference slippage in price points (kept for compatibility with the original expert advisor). |
| `CandleType` | Candle type and timeframe processed by the strategy. |

## Usage Notes
- The strategy is designed for instruments quoted with a fixed price step. Point-based inputs are multiplied by the instrument's `PriceStep` to obtain price distances.
- Order book data is used to estimate the current spread. If best bid/ask quotes are unavailable, spread filtering is skipped.
- Trailing and timeout exits are evaluated on closed candles, matching the original MQL logic.
- Position sizing requires portfolio valuation (`Portfolio.CurrentValue`) when `RiskMode` is set to percentage. If the value is unavailable the strategy falls back to the configured lot size.

## Files
- `CS/V1n1LonnyBreakoutStrategy.cs` – Strategy implementation in C# for StockSharp.
- `README.md` – This description in English.
- `README_cn.md` – 中文简介。
- `README_ru.md` – Русское описание.
