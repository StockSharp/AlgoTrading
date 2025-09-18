# Market Predictor Strategy

## Overview
The Market Predictor strategy is a high-level adaptation of the original MetaTrader MarketPredictor expert advisor. The logic focuses on continuously re-estimating the expected price movement by combining a Monte Carlo forecast with adaptive statistical parameters gathered from recent candles. The strategy subscribes to candles of the selected timeframe and processes only finished bars to avoid premature signals.

## Core Concepts
- **Adaptive mean estimation:** The strategy maintains a dynamic mean price (`mu`) updated from a simple moving average. This mirrors the parameter optimization step from the original expert advisor.
- **Volatility-driven amplitude:** The ATR of the same candle series controls the amplitude coefficient (`alpha`), keeping the prediction responsive to volatility spikes.
- **Monte Carlo projection:** For each completed candle the strategy runs a configurable number of random simulations to estimate the expected price (`P_t1`). The forecast equals the average of the simulated prices.
- **Directional decision:** Market orders are sent when the forecast deviates from the latest close by more than the `sigma` threshold. The position direction is flipped only after the previous exposure is fully closed.

## Trading Rules
1. Wait for the candle to finish and confirm that all indicators are formed.
2. Update `mu` with the SMA value and `alpha` with the ATR-based amplitude.
3. Perform Monte Carlo simulations around the latest close price.
4. If the average simulated price is above `Close + sigma`, enter a long position with a market order when no position is open.
5. If the average simulated price is below `Close - sigma`, enter a short position with a market order when no position is open.
6. Hold the position until the opposite signal is produced.

## Parameters
- **InitialAlpha** – Default amplitude used before the ATR becomes available.
- **InitialBeta** – Placeholder coefficient kept for compatibility with the original Expert Advisor (not used directly in the calculations).
- **InitialGamma** – Placeholder damping constant preserved for documentation consistency (not used directly).
- **Kappa** – Sensitivity parameter for the underlying sigmoid component concept. It is stored for reference and future extensions.
- **InitialMu** – Default mean price until the moving average is formed.
- **Sigma** – Required deviation between the predicted price and the latest close to trigger market entries.
- **MonteCarloSimulations** – Number of simulations used to estimate the next price.
- **CandleType** – Timeframe of the candle series.

## Notes
- The high-level StockSharp API handles candle subscriptions, indicator binding, and market order execution.
- Comments in the source code explain each step of the process for easier maintenance.
- The Python port is intentionally omitted as requested.
