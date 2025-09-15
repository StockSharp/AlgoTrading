# Candle Trend Strategy

## Overview

This strategy opens positions based on the direction of consecutive candles.
A long position is opened after a specified number of bullish candles appears in a row, while a short position is opened after the same number of bearish candles.
Existing positions can be closed when the opposite signal occurs.

## Parameters

- **Candle Type**: Timeframe of candles used for analysis.
- **Trend Candles**: Number of consecutive candles in one direction required to trigger an action.
- **Take Profit %**: Optional take profit expressed as a percentage of entry price.
- **Stop Loss %**: Optional stop loss expressed as a percentage of entry price.
- **Enable Long Entry**: Allow opening long positions.
- **Enable Short Entry**: Allow opening short positions.
- **Enable Long Exit**: Allow closing long positions on opposite signal.
- **Enable Short Exit**: Allow closing short positions on opposite signal.

## Logic

1. Subscribe to candle data of the selected timeframe.
2. Track the number of consecutive bullish and bearish candles.
3. When the bullish counter reaches the required number:
   - Close short positions if allowed.
   - Open a long position if allowed.
4. When the bearish counter reaches the required number:
   - Close long positions if allowed.
   - Open a short position if allowed.
5. Optional protective orders are set using `StartProtection`.

## Notes

- Signals are processed only on finished candles.
- The strategy uses `BuyMarket` and `SellMarket` for entries and exits.
- All comments in the code are written in English as required.
