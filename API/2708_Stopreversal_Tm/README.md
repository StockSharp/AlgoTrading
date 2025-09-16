# Stopreversal Tm Strategy

## Overview
The Stopreversal Tm strategy is a direct translation of the original MetaTrader 5 expert advisor `Exp_Stopreversal_Tm.mq5`. The trading idea follows the Stopreversal custom indicator, which maintains a dynamic trailing stop around price and generates reversal alerts whenever price crosses that trailing boundary. The strategy operates on a single instrument and a single candle feed and is designed for trend reversal trading with a user-defined session filter.

## Signal Generation
The Stopreversal indicator computes a reference price from the selected applied price mode and then adjusts a trailing stop level by `Sensitivity` (the `nPips` parameter). Whenever the new applied price crosses above the trailing stop while the previous bar was below it, a bullish signal is produced. Conversely, a bearish signal appears when the new price drops below the trailing stop after being above it. Each bullish signal simultaneously requests closing of existing short positions and opening of a new long, while each bearish signal closes longs and opens shorts.

To reproduce the behaviour of the original MetaTrader implementation, the strategy can delay the execution of signals by several completed bars (`Signal Bar Delay`). This replicates the `SignalBar` input from the expert advisor and prevents trading on the still-forming candle.

## Session Filter and Position Handling
The expert advisor allowed trading only within a specified time window. The converted strategy keeps the same logic: when the `Use Time Filter` flag is enabled, orders are allowed only inside the session configured by `Start Hour/Minute` and `End Hour/Minute`. If the current time leaves the permitted window, any open position is flattened immediately. Signal-driven exits remain active even when the session is disabled.

The strategy works on net positions. A closing action is always executed before an opposite entry, guaranteeing that the direction changes without overlapping exposures.

## Parameters
- **Allow Buy Entries / Allow Sell Entries** – enable or disable opening new long or short positions when the corresponding signal is received.
- **Allow Long Exits / Allow Short Exits** – control whether opposite signals are allowed to close existing positions.
- **Use Time Filter** – toggles the trading session window.
- **Start Hour / Start Minute / End Hour / End Minute** – define the inclusive start and exclusive end of the trading window. The time filter supports overnight sessions where the end time is earlier than the start time.
- **Sensitivity (`nPips`)** – relative distance (expressed as a multiplier, e.g., `0.004 = 0.4%`) used to move the trailing stop closer or further from price.
- **Signal Bar Delay (`SignalBar`)** – number of completed candles to wait before acting on a signal. `0` executes immediately on the closing candle, `1` reproduces the default behaviour of acting on the previous bar.
- **Candle Type** – timeframe of the candle subscription used for indicator calculations.
- **Applied Price** – choice of price series (close, open, median, trend-following modes, Demark price, etc.) that feeds the trailing stop calculation.

## Implementation Notes
- The indicator is implemented directly inside the strategy without relying on external buffers, ensuring that the `nPips` trailing stop logic matches the original MQL5 code.
- Session management and signal sequencing follow the original expert, including the priority of closing existing exposure before opening new trades.
- The conversion focuses on the high-level StockSharp API: candle subscriptions, delayed signal queue, and market orders (`BuyMarket` / `SellMarket`). Money management features tied to MetaTrader account metrics were omitted because StockSharp strategies already operate with explicit position sizes.
