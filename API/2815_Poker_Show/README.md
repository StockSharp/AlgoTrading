# Poker Show Strategy

## Overview

The Poker Show strategy is a direct port of the MetaTrader 5 expert advisor "Poker_SHOW". It combines a moving average trend filter with a probabilistic trigger that mimics drawing a poker hand. Trades are executed only when the randomly generated hand value falls below a configurable poker combination threshold. The approach produces infrequent entries while staying aligned with the prevailing trend detected by the moving average.

The strategy works on a single symbol and relies on regular time-based candles. Trading decisions are evaluated once per completed candle, which matches the original advisor that reacts on the opening of each new bar.

## Core Logic

1. **Moving Average Trend Filter**
   - A configurable moving average (SMA, EMA, SMMA, or LWMA) is calculated from the selected price source (close, open, high, low, median, typical, or weighted price).
   - The indicator can be shifted forward in time to reproduce the MetaTrader "shift" input. The strategy always uses the value from the previous fully formed candle, just like the source EA.

2. **Probability Gate**
   - Each side (long or short) draws an independent random value between 0 and 32,767 on every bar.
   - The draw is compared with the selected poker combination. Higher-ranked combinations (e.g., straight flush) have smaller numeric thresholds and therefore trigger less frequently, while lower-ranked combinations (e.g., one pair) trade more often.

3. **Directional Rules**
   - Long trades require the moving average to stay above the price by at least the configured distance. When the **Reverse Signals** option is enabled, the condition is inverted.
   - Short trades require the moving average to stay below the price by the same margin, with the condition inverted when the reverse switch is active.
   - Only one position can be active at a time. Entering in the opposite direction automatically offsets any open exposure before establishing the new trade.

4. **Risk Management**
   - Optional stop loss and take profit levels are calculated in price steps (points) relative to the execution price. Setting a distance to zero disables the corresponding level.
   - Stops and targets are checked on every completed candle. When hit, the strategy closes the position and resets risk markers.

5. **Position Protection**
   - The built-in StockSharp protection module is activated on start to preserve the account from unexpected losses during manual runs.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Poker Combination** | Probability threshold that must exceed the random draw to allow a new trade. Represents classic poker hands from straight flush (rarest) to one pair (most common). |
| **Volume** | Order volume in lots. Used both for fresh entries and for reversing existing positions. |
| **Stop Loss** | Distance between the entry price and the protective stop, measured in price steps. Set to zero to disable. |
| **Take Profit** | Distance between the entry price and the profit target, measured in price steps. Set to zero to disable. |
| **Enable Buy** | Allows the strategy to open long positions. |
| **Enable Sell** | Allows the strategy to open short positions. |
| **MA Distance** | Minimum distance in price steps between the moving average value and the current price. Acts as a trend confirmation filter. |
| **MA Period** | Number of bars used by the moving average. |
| **MA Shift** | Horizontal shift applied to the moving average (in bars), matching the MetaTrader `ma_shift` input. |
| **MA Method** | Moving average smoothing type: simple, exponential, smoothed, or linear weighted. |
| **Applied Price** | Candle price used in the moving average calculation. |
| **Reverse Signals** | Inverts the comparison between the moving average and price, effectively swapping long and short logic. |
| **Candle Type** | Time frame of the candle subscription. Default is one hour to replicate the original settings. |

## Notes and Recommendations

- The probability gate makes the strategy highly stochastic. Backtests should use multiple runs or Monte Carlo analysis to understand the distribution of outcomes.
- Because trade management relies on completed candles, large intrabar spikes may overshoot stop or target levels before the strategy can react. Consider running on lower time frames if this behavior is undesirable.
- To reproduce the MetaTrader environment faithfully, ensure the instrument uses the same contract size and price step so that point-based distances match the original lots and pip values.
- The strategy uses market orders (`BuyMarket` and `SellMarket`) as in the source expert advisor. Slippage handling is delegated to the StockSharp execution infrastructure.
