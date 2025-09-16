# GBP 9 AM Breakout Strategy

## Overview

The **GBP 9 AM Breakout Strategy** replicates the legacy MetaTrader "GBP9AM" expert advisor in StockSharp. The system prepares a straddle around the London open (9:00 local time) by placing buy-stop and sell-stop orders at configurable distances from the current price. It aims to capture the post-open momentum move while enforcing disciplined risk management through stop-loss and take-profit levels measured in pips.

## Trading Logic

1. The strategy monitors finished candles of a configurable timeframe (1-minute by default) to work with exchange timestamps.
2. Each new trading day resets the setup state so only one straddle is prepared per session.
3. Once the candle time reaches the configured "Look Hour" and "Look Minute", the strategy:
   - Cancels any remaining active orders and closes open positions to avoid conflicts.
   - Calculates pip-adjusted entry, stop-loss, and take-profit prices using the security's price step.
   - Places both a buy-stop and a sell-stop order at the specified pip distances from the latest close price.
4. When one side fills, the opposite pending order is cancelled immediately. The strategy then tracks price action to exit the position once either the stop-loss or take-profit level is hit intraday.
5. An optional daily "Close Hour" forces the strategy to flatten positions and remove pending orders at the end of the London session.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Order size used for both sides of the straddle.
| `LookHour` | Exchange hour (0-23) that represents 9 AM London in your data feed.
| `LookMinute` | Minute offset within the look hour when orders should be prepared.
| `CloseHour` | Hour at which all positions and orders are forcefully closed.
| `UseCloseHour` | Enables or disables the automatic close hour behaviour.
| `TakeProfitPips` | Distance in pips from the entry price to the profit target for both directions.
| `BuyDistancePips` | Pip distance above the current price for the buy-stop order.
| `SellDistancePips` | Pip distance below the current price for the sell-stop order.
| `BuyStopLossPips` | Stop-loss distance in pips for long positions.
| `SellStopLossPips` | Stop-loss distance in pips for short positions.
| `CandleType` | Candle subscription used for timing and exit management (defaults to 1-minute time frame).

All pip distances automatically adapt to 3- or 5-digit FX quotes by multiplying the exchange price step by ten where needed, mirroring the original expert advisor.

## Risk Management

- The strategy always issues symmetrical stop-loss and take-profit targets around the trigger price to maintain a balanced risk profile.
- End-of-day liquidation ensures the account does not carry overnight exposure unless the `UseCloseHour` parameter is disabled.
- Because orders are restated only once per day, the strategy avoids over-trading during ranging sessions.

## Usage Notes

1. Set the `LookHour` to match 9 AM London time within your broker's time zone. For example, if the feed is UTC+1, use `LookHour = 10`.
2. Calibrate pip distances to accommodate the current volatility of GBP/USD or your preferred GBP pair.
3. Deploy the strategy on FX symbols that expose reliable bid/ask and price step metadata so that pip calculations remain accurate.
4. Monitor broker margins: larger `Volume` values may require adjustments in account leverage just like the original MQL version did.

## Files

- `CS/Gbp9AmBreakoutStrategy.cs` – C# implementation using the StockSharp high-level API.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.

Python implementation is intentionally omitted per project requirements.
