# Bulls vs Bears Crossover Strategy

## Overview

This strategy implements a crossover system based on the **Bulls vs Bears (BvsB)** indicator. The indicator measures the distance between a candle's high and low prices and a moving average. When the bullish distance drops below the bearish distance, it indicates fading upward pressure, and the strategy opens a long position. Conversely, when the bullish distance rises above the bearish distance, a short position is opened. Existing positions are closed on the opposite signal or when profit or loss targets are reached.

The moving average type and length are configurable, allowing the strategy to adapt to different markets and timeframes. Risk management is controlled through fixed stop-loss and take-profit levels expressed in price steps.

## Parameters

| Name | Description |
|------|-------------|
| `MaType` | Moving average calculation method (SMA, EMA, SMMA, WMA). |
| `MaLength` | Period of the moving average. |
| `StopLoss` | Stop-loss distance in price steps. |
| `TakeProfit` | Take-profit distance in price steps. |
| `OpenLong` | Allow opening long positions on bullish crossover. |
| `OpenShort` | Allow opening short positions on bearish crossover. |
| `CloseLong` | Allow closing long positions on bearish crossover. |
| `CloseShort` | Allow closing short positions on bullish crossover. |
| `CandleType` | Timeframe of processed candles. |

## How It Works

1. Subscribe to the specified candle series and calculate a moving average.
2. For each finished candle, compute the bullish and bearish distances:
   - **Bull** = `(HighPrice - MA) / PriceStep`
   - **Bear** = `(MA - LowPrice) / PriceStep`
3. Detect crossovers between the Bull and Bear values.
4. Open or close positions according to crossover direction and enabled options.
5. Manage risk using the configured stop-loss and take-profit levels.

This simple yet flexible approach can be applied to many instruments to gauge the balance between bullish and bearish forces.
