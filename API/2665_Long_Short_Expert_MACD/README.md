# Long/Short Expert MACD Strategy

## Overview
The **Long/Short Expert MACD Strategy** is a StockSharp conversion of the MetaTrader expert "LongShortExpertMACD". It combines the standard Moving Average Convergence Divergence (MACD) crossover logic with fixed-distance risk controls. The strategy reacts to crossovers between the MACD line and its signal line, can operate in long-only, short-only, or bidirectional modes, and automatically applies take-profit and stop-loss levels expressed in price points.

The implementation uses the high-level StockSharp API with candle subscriptions and indicator bindings. Orders are registered as market orders, making the strategy simple to connect to both real-time and historical data sources.

## Indicators and Market Data
- **Candles** – a single timeframe provided by the `CandleType` parameter (1-minute time frame by default). The strategy subscribes to this candle series via `SubscribeCandles`.
- **MovingAverageConvergenceDivergenceSignal** – StockSharp's MACD indicator with configurable fast EMA, slow EMA, and signal EMA lengths. The histogram value is implicitly derived from the difference between the MACD and signal outputs.

## Trading Logic
1. **Signal preparation**
   - On every finished candle the MACD and signal values are retrieved through the indicator binding.
   - Historical state `_prevIsMacdAboveSignal` tracks whether MACD was above the signal line during the previous candle.

2. **Entry conditions**
   - **Bullish crossover**: when MACD crosses above the signal line, the strategy opens a long position if the configured trade direction allows long entries.
     - If a short position is already active and reversal mode is enabled (`AllowedPosition = Both`), the order size includes the current short volume to close the position and flip to long in a single market order.
     - In long-only mode an existing short position is immediately closed, but no new long trade is opened until the following signal.
   - **Bearish crossover**: the symmetric action for short entries.

3. **Exit conditions**
   - **Risk management**: both stop-loss and take-profit levels are recomputed from the current average entry price each time a position is detected. The distances are set in price points (i.e., `Security.PriceStep * parameter`), which keeps the behaviour consistent across instruments.
     - Long positions exit when the candle's low reaches the stop-loss level or the high reaches the take-profit level.
     - Short positions exit when the candle's high reaches the stop-loss level or the low touches the take-profit level.
   - **Opposite crossover**: if trade direction permits the opposite side, the position is flattened (and optionally reversed) whenever the indicator relationship flips.

4. **Operational safeguards**
   - Trading logic is executed only when the strategy is formed, online, and trading is allowed (`IsFormedAndOnlineAndAllowTrading`).
   - Protection levels are reset whenever no position is held to avoid stale thresholds.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `AllowedPosition` | `Both` | Restricts the strategy to long-only, short-only, or bidirectional trading. |
| `FastLength` | `12` | Period of the fast EMA within the MACD calculation. |
| `SlowLength` | `24` | Period of the slow EMA within the MACD calculation. |
| `SignalLength` | `9` | Period of the signal EMA used for crossover detection. |
| `TakeProfitPoints` | `50` | Distance to the take-profit level measured in price points (`PriceStep * points`). Set to `0` to disable. |
| `StopLossPoints` | `20` | Distance to the stop-loss level measured in price points. Set to `0` to disable. |
| `CandleType` | `TimeFrame(1 minute)` | Candle series used for signal generation. |
| `Volume` | `1` | Number of lots/contracts sent with each market order. |

All numeric parameters expose optimization ranges to simplify walk-forward testing within StockSharp Designer or the Runner.

## Position Management
- **Reversal logic**: when bidirectional trading is allowed the strategy uses combined order sizes to flip positions in a single market order, mirroring the behaviour of the original MetaTrader expert.
- **Long-only / short-only modes**: existing positions on the disallowed side are closed immediately, but no new exposure is established until a signal aligned with the permitted direction occurs.
- **Stop/take recalculation**: the strategy recalculates protection levels on each candle using the latest `PositionAvgPrice`, ensuring correct distances even after partial fills or scaled entries.

## Usage Notes
- Ensure the instrument provides a valid `PriceStep`; if the value is missing the strategy falls back to `1.0` price units, which is appropriate for equity-style instruments but may require adjustment for Forex symbols.
- The strategy relies on completed candles. Latency-sensitive scenarios should supply appropriately granular candles to avoid delays.
- Because orders are market orders without slippage controls, risk management should consider potential fill differences, especially on illiquid assets.
- Visualisation is automatically created when the host application supports chart areas; MACD, candles, and own trades are drawn for quick monitoring.

## Conversion Notes
- The StockSharp implementation preserves the configurable MACD parameters, take-profit and stop-loss distances, and the position-availability switch from the MQL5 expert.
- Trailing-stop and money-management modules used in MetaTrader are intentionally omitted because their behaviour is equivalent to the "none" variants included with the original expert.
