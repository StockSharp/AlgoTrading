# BnB Strategy

This strategy is a port of the MetaTrader 5 Expert Advisor "Exp_BnB". It operates on the custom BnB (Bulls and Bears) indicator that measures bullish and bearish pressure inside each candle and smooths them with an exponential moving average.

## How it works

1. For each finished candle the strategy calculates bulls and bears values.
2. Both series are smoothed with EMA.
3. When the bulls line crosses above the bears line:
   - Any short position is closed.
   - A long position is opened.
4. When the bears line crosses above the bulls line:
   - Any long position is closed.
   - A short position is opened.
5. Stop loss and take profit levels are managed in absolute price points.

## Parameters

- `Candle Type` – time frame of the candles used for calculations.
- `EMA Length` – smoothing period for bulls and bears.
- `Stop Loss` – distance to the protective stop in price points.
- `Take Profit` – distance to the profit target in price points.
- `Allow Long Entry` – enable long position opening.
- `Allow Short Entry` – enable short position opening.
- `Allow Long Exit` – enable long position closing.
- `Allow Short Exit` – enable short position closing.

## Notes

The original indicator supports multiple smoothing methods. In this port the universal filter is approximated with a standard exponential moving average.
