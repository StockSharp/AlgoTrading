# Matrix Machine Learning Strategy

## Overview
Matrix Machine Learning is a neural-network-based approach originally published for MetaTrader 5 inside the "MQL5Book" educational project. The expert script collects a window of tick prices, converts consecutive price differences into a binary sequence, and trains a Hopfield recurrent neural network. The trained network is evaluated on an in-sample segment, validated on an out-of-sample segment, and finally used to infer the direction of the next movements. Positions are opened when the first element of the forecasted binary vector shows a bullish (`+1`) or bearish (`-1`) direction.

This C# version ports the original logic to the StockSharp high-level API and replaces tick processing with finished candles to ensure stable cross-platform behaviour. Every candle close updates the binary price pattern, retrains the Hopfield network, evaluates historical accuracy, and produces an online forecast for the upcoming steps.

## Algorithm Details
1. Collect the latest `HistoryDepth` candle closes. The newest `ForwardDepth` points form the out-of-sample set, while the remaining values create the training segment.
2. Convert consecutive close-to-close differences into a binary sequence: positive or zero deltas become `+1`, negative deltas become `-1`.
3. Train a Hopfield weight matrix by summing the outer products of every predictor/output pair where the predictor length equals `PredictorLength` and the response length equals `ForecastLength`.
4. Evaluate the trained matrix on the training and forward sets. The accuracy metric matches the original script: the dot product between predicted and actual response vectors is averaged and rescaled to a percentage.
5. Build the latest online binary pattern and run the Hopfield inference loop (tanh activation with a convergence threshold). The first forecast component drives the trading decision.

## Parameters
- **History Depth** – number of recent candle closes stored for the Hopfield network. Must be larger than `ForwardDepth` and at least `PredictorLength + ForecastLength + 1`.
- **Forward Depth** – size of the validation window reserved for forward checks. Requires at least `ForecastLength + 1` closes.
- **Predictor Length** – length of the binary input vector used by the neural network.
- **Forecast Length** – number of future steps predicted by the network output vector.
- **Candle Type** – StockSharp `DataType` describing the candle series requested from the connector.
- **Debug Log** – when enabled, prints detailed intermediate vectors, sample comparisons, and online forecasts.

## Trading Logic
- If the first element of the Hopfield forecast is positive and the strategy is flat or short, a market buy order is submitted for `Volume + |Position|` to flip into a long position.
- If the first element is negative and the strategy is flat or long, a market sell order is submitted for `Volume + |Position|` to flip into a short position.
- Zero forecasts are ignored to avoid unnecessary churn.

The strategy automatically plots candles and own trades when a chart area is available. The Hopfield network retrains on every finished candle to keep the neural weights synchronized with the most recent market structure.
