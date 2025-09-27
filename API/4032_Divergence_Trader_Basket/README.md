# Divergence Trader Basket Strategy

## Overview
This strategy is a StockSharp port of the "Divergence Trader" MetaTrader expert advisor. It compares two simple moving averages
calculated on configurable price sources and measures their difference (divergence). When the distance between the fast and slow
averages falls inside a neutral corridor, the algorithm assumes that momentum is about to resume and opens a position in the
direction of the prevailing bias. The implementation uses only completed candles from a selected timeframe and relies on the
high-level API with indicator bindings.

## Parameters
- **Lot Size** – trading volume submitted with each new position. The value is aligned with the instrument volume step.
- **Fast SMA Period / Price** – length and price source for the fast moving average.
- **Slow SMA Period / Price** – length and price source for the slow moving average.
- **Buy Threshold** – minimal positive divergence required before opening a long position.
- **Stay-Out Threshold** – maximal divergence allowed for new entries; values outside this range disable trading.
- **Take Profit (pips)** – profit target expressed in pips. Disabled when set to zero.
- **Stop Loss (pips)** – loss tolerance in pips. Disabled when set to zero.
- **Trailing Stop (pips)** – trailing distance activated after the trade becomes profitable. Disabled when zero.
- **Break-Even Trigger / Buffer (pips)** – pip gain required before protecting the position at break-even and optional buffer to
  offset the break-even stop from the entry price.
- **Basket Profit / Basket Loss** – account-equity based thresholds that flatten all positions when reached. Loss control is
disabled by default.
- **Start Hour / Stop Hour** – trading window in local time. When both values are equal the strategy operates all day.
- **Candle Type** – timeframe used for both signal generation and risk management.

## Trading Logic
1. Subscribe to the configured candle series and calculate the fast and slow simple moving averages.
2. Work only with finished candles to avoid intrabar noise and to stay close to the original EA behaviour.
3. Track the divergence (fast minus slow) computed on the previously finished candle:
   - If the divergence is positive and remains between the **Buy Threshold** and **Stay-Out Threshold**, submit a market buy order.
   - If the divergence is negative and its absolute value stays inside the corridor, submit a market sell order.
4. Trades are ignored outside the allowed hours or when the strategy already has an open position.

## Position Management
- **Break-even control** – when the floating profit reaches the trigger, the strategy stores a break-even stop level (optionally
  shifted by the buffer). A candle that touches this level closes the position.
- **Trailing stop** – once profit exceeds the trailing distance, the stop level follows the most favourable price while always
  staying behind it by the configured number of pips.
- **Take profit / stop loss** – fixed exits calculated from the entry price in pip units.
- **Basket protection** – portfolio equity is compared against the configured profit and loss limits. Hitting either boundary
  closes the current position and cancels active orders, emulating the "CloseEverything" routine from the MQL version.

## Usage Notes
- The divergence corridor is symmetric: widening the **Stay-Out Threshold** allows trades to stay open longer, while narrowing it
  increases the frequency of signals.
- Price source options correspond to StockSharp `CandlePrice` values, making it possible to use open, close, median or typical
  prices just like in MetaTrader.
- The strategy plots candles, both moving averages and filled orders on a chart area for monitoring and debugging.
- Money-management features depend on portfolio data. When running in a sandbox without portfolio statistics, basket controls are
  ignored automatically.
