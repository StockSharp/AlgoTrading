# Cronex DeMarker Crossover Strategy

## Overview
The Cronex DeMarker Crossover Strategy reproduces the MetaTrader indicator **Cronex DeMarker** and transforms it into an automated trading system. The original indicator plots the DeMarker oscillator together with two linear weighted moving averages (LWMAs). The strategy mirrors that setup, evaluates bullish and bearish crossovers between the smoothed oscillator lines, and converts them into market orders. This allows the trading logic to react immediately when momentum shifts from downside to upside pressure (and vice versa) according to the indicator.

## Indicator construction
1. **DeMarker oscillator** – Measures the relationship between the current candle and the previous candle:
   - If the current high is higher than the previous high, the positive pressure equals the difference of the highs; otherwise it is zero.
   - If the current low is lower than the previous low, the negative pressure equals the distance between the lows; otherwise it is zero.
   - The sums of positive and negative pressure over `DeMarkerPeriod` bars form the oscillator value `deMax / (deMax + deMin)`.
2. **Fast LWMA** – A linear weighted moving average with period `FastMaPeriod` is applied to the raw DeMarker values in order to emphasise the latest oscillator changes.
3. **Slow LWMA** – Another linear weighted moving average with period `SlowMaPeriod` smooths the same DeMarker stream to build a slower confirmation line.

The strategy feeds every finished candle to this indicator stack, exactly matching the buffer calculations from the original MQ4 file.

## Trading logic
1. Wait until the DeMarker oscillator and both LWMAs are fully formed.
2. After each completed candle, compute the fresh DeMarker value and update both moving averages.
3. Detect crossovers between the fast and slow LWMA series:
   - **Bullish crossover** – The fast LWMA moves from below to above the slow LWMA. The strategy closes any short exposure and opens a long market position.
   - **Bearish crossover** – The fast LWMA moves from above to below the slow LWMA. The strategy closes any long exposure and opens a short market position.
4. Orders are skipped while the strategy is not yet formed, while it is offline, or when trading is disabled.

Positions are reversed immediately on opposite signals. Existing exposure is closed by adding the required quantity to the new market order.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `DeMarkerPeriod` | Number of candles used to build the DeMarker oscillator. | `25` |
| `FastMaPeriod` | Period of the fast linear weighted moving average that reacts to new oscillator values. | `14` |
| `SlowMaPeriod` | Period of the slow linear weighted moving average that confirms the direction. | `25` |
| `CandleType` | Candle series processed by the strategy (time-frame or other `DataType`). | `1 Hour` time-frame |

## Implementation details
- Uses the high-level `SubscribeCandles` API. Indicators are updated only when a candle reaches the `Finished` state to avoid mid-bar repainting.
- The strategy relies on the built-in `DeMarker` and `WeightedMovingAverage` indicators from StockSharp to faithfully replicate the MQ4 buffers.
- A chart area is created automatically, plotting the price candles together with the oscillator and both moving averages for visual confirmation.
- `StartProtection()` is invoked during startup so that position protection is engaged exactly once, as required by the project guidelines.

## Usage
1. Attach the strategy to the desired security and assign the preferred candle type (for example, 1-hour time-frame candles).
2. Configure the DeMarker and moving average periods to match the original indicator or tune them for optimisation.
3. Run the strategy. It will start trading once the indicators are fully formed and trading is allowed.
4. Monitor the plotted chart to see the DeMarker oscillator and LWMA crossover signals driving the entries.
