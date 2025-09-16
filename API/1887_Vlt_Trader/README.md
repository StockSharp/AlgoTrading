# Vlt Trader

This strategy detects periods of very low volatility and prepares breakout orders. When the range of the current candle becomes the smallest over the specified lookback period, the strategy places buy stop and sell stop orders around the previous candle.

## Parameters
- **Period** – lookback period for the minimum range calculation.
- **Pending level** – distance in ticks from the previous high/low to place stop orders.
- **Stop loss** – protective stop in ticks.
- **Take profit** – profit target in ticks.
- **Candle type** – timeframe used for analysis.

## Logic
1. For each finished candle, compute its range (`High - Low`).
2. Track the smallest range over the last *Period* candles.
3. When the current range sets a new minimum, cancel existing orders and place stop orders above and below the previous candle at the given offset.
4. `StartProtection` manages stop-loss and take-profit once a position is opened.
