# Simplest DeMarker Strategy

## Overview
The Simplest DeMarker Strategy reproduces the logic of the original MetaTrader expert advisor. It tracks the DeMarker oscillator to detect when price momentum leaves overbought or oversold zones. When the oscillator crosses back inside the neutral range, the strategy opens a position in the direction of the expected reversal while managing risk via configurable stop-loss and take-profit distances.

## Core Logic
1. Subscribe to candles of the selected timeframe and calculate the DeMarker indicator with the configured period.
2. Mark the market state as **overbought** whenever the previous DeMarker value is above the overbought threshold and as **oversold** when it is below the oversold threshold.
3. Generate signals when the current DeMarker value crosses back inside the neutral area:
   - Sell when the oscillator falls below the overbought level after previously being above it.
   - Buy when the oscillator rises above the oversold level after previously being below it.
4. Place only one position at a time. If `Trade On Bar Open` is enabled, the order is delayed until the next bar opens; otherwise, the position is entered immediately on the current bar close.
5. Apply stop-loss and take-profit orders using the built-in protection service to mimic the fixed distances from the MQL version.

## Parameters
- **Volume** – order size in lots/contracts.
- **DeMarker Period** – period of the DeMarker oscillator.
- **Overbought Level** – upper DeMarker threshold that defines overbought conditions.
- **Oversold Level** – lower DeMarker threshold that defines oversold conditions.
- **Trade On Bar Open** – if enabled, entries are executed on the next bar open rather than immediately.
- **Stop Loss Points** – protective stop-loss distance expressed in price points.
- **Take Profit Points** – profit target distance expressed in price points.
- **Candle Type** – candle type (timeframe) used for indicator calculations.

## Money Management
- Stop-loss and take-profit orders are registered automatically through `StartProtection` with distances converted to price points.
- Only one position can be active at a time. New signals are ignored while a position exists.

## Chart Elements
- Price candles for the selected subscription.
- The DeMarker indicator curve.
- Own trades markers for visual validation of entries and exits.

## Notes
- Use sufficiently high liquidity instruments to ensure stop-loss and take-profit execution quality.
- The `Trade On Bar Open` flag approximates the original expert advisor behaviour that waits for a new bar before sending the order.
