# Galactic Explosion Strategy

## Overview
The Galactic Explosion strategy rebuilds the original MetaTrader 5 grid expert in StockSharp. It operates on finished candles, uses a long-term moving average to define the directional bias, and deploys an expanding grid of orders. The system accumulates trades when price stays on one side of the moving average and closes the entire basket once a predefined profit target is achieved.

## Market Logic
1. **Directional filter** – the strategy compares the latest candle close with a moving average. When price closes below the average the bias turns bullish, and when price closes above the average the bias becomes bearish.
2. **Progressive grid** – the first eight entries are taken whenever the bias allows. After the eighth position the distance between the current price and both the last and first entries controls whether additional trades are allowed.
3. **Spacing control** – distances are measured in price steps. If price has moved far enough from the last entry the strategy will add to the basket. Depending on the distance to the very first entry it will either trade immediately, skip three candles, or skip six candles before adding again.
4. **Profit realisation** – realised PnL plus the open profit of the basket is compared to the minimal profit target. When the threshold is met every open position is closed in a single market order.

## Trade Management
- **Entry volume** – every trade is executed with the configured order volume. When the signal flips while holding a position the strategy sends a single order that closes the old side and opens a new one with the required extra volume.
- **Position tracking** – the strategy keeps the average price and the first/last entry price for long and short baskets independently. This allows it to reproduce the distance-based scaling rules of the original expert.
- **Session filter** – trading is only active between the configured start and end hours. The logic uses the candle opening time and ignores signals outside of this window.
- **Safety check** – if the trading window is misconfigured (for example, the start hour is not earlier than the end hour) the strategy skips trading and logs a warning.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Order Volume** | Volume used for each new entry. This value is also used to estimate how many grid steps are currently open. |
| **Start Hour** | Start of the trading session in exchange time. Signals before this hour are ignored. |
| **End Hour** | End of the trading session (exclusive). Signals after this hour are ignored. |
| **Minimal Profit** | Combined realised plus unrealised profit that triggers closing all open positions. |
| **Indent After 8th** | Minimum distance (in price steps) from the most recent entry after eight trades before another position can be opened. |
| **Skip 3 Min** | Lower bound (in price steps) for activating the “skip three candles” rule. |
| **Skip 3 Max** | Upper bound (in price steps) that keeps the “skip three candles” rule active. |
| **Skip 6 Max** | Upper bound (in price steps) that keeps the “skip six candles” rule active. |
| **MA Length** | Length of the simple moving average that defines the directional bias. |
| **Candle Type** | Candle series subscribed by the strategy. The moving average and grid logic run on this data stream. |

## Implementation Notes
- The strategy uses `SubscribeCandles` with a `SimpleMovingAverage` indicator and processes only finished candles.
- Position statistics are maintained through `OnNewMyTrade`, enabling precise tracking of the first and last entry prices as well as average prices for open baskets.
- Distance thresholds are scaled by the security `PriceStep`, reproducing the original pip-based configuration of the MT5 expert.
- The implementation avoids custom collections and focuses on scalar state variables to comply with the project guidelines.
