# Hull Trend OSMA Strategy

This strategy is a conversion of the MetaTrader "Exp_HullTrendOSMA" expert advisor.

## Overview

The strategy uses the Hull Trend OSMA indicator, which calculates a Hull Moving Average and a smoothed version of it. The oscillator value is the difference between these two series. When the oscillator rises for two consecutive completed candles the strategy opens a long position. When the oscillator falls for two consecutive completed candles the strategy opens a short position. Opposite positions are closed on each signal.

## Parameters

- **Hull Period** – period for the Hull Moving Average.
- **Signal Period** – period of the smoothing moving average applied to the oscillator.
- **Take Profit** – distance for protective take profit orders in price units.
- **Stop Loss** – distance for protective stop loss orders in price units.
- **Candle Type** – timeframe of candles used for calculations (default 8 hours).

## Notes

- Uses high level StockSharp API with automatic candle subscription.
- Entries and exits are executed with market orders.
- Stop loss and take profit protection is initialized once when the strategy starts.
