# Donchian Channels System

The **Donchian Channels System** strategy trades breakouts of the Donchian Channel with an optional shift to avoid look-ahead bias.

## How It Works
- **Long entry**: when the close price crosses above the upper Donchian band calculated `Shift` bars ago.
- **Short entry**: when the close price crosses below the lower Donchian band calculated `Shift` bars ago.
- Positions are reversed on opposite breakout.

## Parameters
- `DonchianPeriod` = 20
- `Shift` = 2
- `CandleType` = 4h

## Indicators
- Donchian Channel
