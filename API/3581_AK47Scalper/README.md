# AK-47 Scalper Strategy

## Overview
The **AK-47 Scalper Strategy** is a high-frequency pending order system converted from the MQL4 expert advisor "AK-47 Scalper EA". The strategy constantly keeps a stop order on the book: either a sell-stop below the market or a buy-stop above it. The activation price is always offset by half of the configured stop-loss distance, which allows the order to follow the current spread and grab breakouts in liquid sessions.

## Trading Logic
1. Subscribe to the selected candle series (one-minute candles by default) and update the best bid/ask quotes from the order book.
2. During the allowed trading window the strategy maintains a single pending stop order in the configured direction. The activation price stays half the stop-loss distance away from the current bid/ask, mirroring the MetaTrader implementation.
3. When the stop order is filled the strategy immediately recreates the protective stop-loss and optional take-profit orders. Stops trail towards the price whenever the market moves far enough, keeping the same pip distance as in the original EA.
4. If trading time ends or the spread exceeds the allowed threshold, active pending orders are cancelled. Once the conditions normalize a new pending order is submitted automatically.

## Risk Management
- **StopLossPips** – defines the stop-loss distance in pips. The value also controls the offset for the pending order. When trailing is enabled the stop follows the price while preserving the same pip gap.
- **TakeProfitPips** – optional pip-based profit target. Set it to `0` to disable the target while keeping the trailing stop.
- **UseRiskPercent / RiskPercent / FixedVolume** – reproduce the original volume calculation. When `UseRiskPercent` is enabled the lot size is derived from the free capital and the configured risk percentage; otherwise `FixedVolume` is used directly.
- **MaxSpreadPoints** – blocks new entries whenever the current spread (expressed in price points) is larger than the threshold. The conversion from points to price takes the security price step into account.
- Protective orders are always recreated after partial fills to keep the full position covered.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Direction` | Choose between a sell-stop (short breakout) or buy-stop (long breakout). | `SellStop` |
| `UseRiskPercent` | Toggle for lot sizing based on risk percentage. | `true` |
| `RiskPercent` | Percentage of free capital used to size positions. | `3` |
| `FixedVolume` | Base lot when percentage risk management is disabled. | `0.01` |
| `StopLossPips` | Stop-loss distance and order offset in pips. | `3.5` |
| `TakeProfitPips` | Optional take-profit distance in pips. | `7` |
| `MaxSpreadPoints` | Maximum allowed spread expressed in price points. | `5` |
| `UseTimeFilter` | Enable or disable the trading session filter. | `true` |
| `StartHour` / `StartMinute` | Session opening time (platform time zone). | `02:30` |
| `EndHour` / `EndMinute` | Session closing time. | `21:00` |
| `CandleType` | Candle data type used for management events. | `1 minute` time frame |

## Notes
- The pip conversion multiplies the price step by ten for 5-digit and 3-digit instruments, matching the MetaTrader behaviour.
- The strategy relies on live order book updates; ensure the selected data source provides best bid/ask quotes.
- No Python port is included; only the C# implementation is provided in this directory.
