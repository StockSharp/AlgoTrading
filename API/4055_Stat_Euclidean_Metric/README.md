# Stat Euclidean Metric Strategy

## Overview
The strategy reproduces the behaviour of the MetaTrader expert advisor `Stat_Euclidean_Metric.mq4`. It monitors MACD reversals on a single instrument and timeframe. When the MACD line forms a local turning point the strategy either opens a position immediately (training mode) or validates the setup with a k-nearest neighbours (k-NN) classifier that compares the current market structure with historical feature vectors stored in binary files.

## Trading logic
1. Subscribe to the configured candle type and calculate the MACD indicator on the typical price ( (High + Low + Close) / 3 ).
2. Detect a bearish reversal when the last three completed MACD values satisfy `MACD[2] <= MACD[1]` and `MACD[1] > MACD[0]`.
3. Detect a bullish reversal when `MACD[2] >= MACD[1]` and `MACD[1] < MACD[0]`.
4. Depending on the selected mode:
   - **Training mode (`TrainingMode = true`)** – open a market order in the direction of the reversal after optionally closing the current position. This mimics the original EA behaviour when it gathers new samples.
   - **Classifier mode (`TrainingMode = false`)** – compute five ratios of simple moving averages of the typical price and evaluate the probability of success with a k-NN model. Place orders only if the probability crosses the configured thresholds.
5. Apply the built-in `StartProtection` module to attach stop-loss and take-profit levels in instrument steps.

## Feature vector for classification
The k-NN model uses the following ratios calculated on the just closed candle:
- SMA(89) / SMA(144)
- SMA(144) / SMA(233)
- SMA(21) / SMA(89)
- SMA(55) / SMA(89)
- SMA(2) / SMA(55)

Each sample stored in the dataset files contains six `double` values: the five ratios above and a label (`0` for an unfavourable outcome, `1` for a successful trade). During evaluation the strategy selects the closest `NeighborCount` samples, averages their labels and interprets the result as the probability of success.

## Dataset files
- `BuyDatasetPath` – path to the binary file with vectors collected after bullish trades.
- `SellDatasetPath` – path to the binary file with vectors collected after bearish trades.

If a path is relative it will be resolved against `Environment.CurrentDirectory`. Missing files are reported in the log and treated as an empty dataset. This implementation reads datasets but does not update or append new samples automatically; exporting new vectors must be handled externally when running in training mode.

## Parameters
- **TrainingMode** – switch between pure MACD trading and classifier-assisted trading.
- **BuyThreshold / SellThreshold** – minimal probability returned by the classifier to open trades in the primary direction.
- **AllowInverseEntries** – enables contrarian trades when the probability is extremely low.
- **InverseBuyThreshold / InverseSellThreshold** – maximal probability that still triggers an opposite-direction trade.
- **FastLength / SlowLength / SignalLength** – MACD EMA lengths.
- **TakeProfitPoints / StopLossPoints** – protective levels expressed in instrument steps.
- **ClosePositionsOnSignal** – close the current net position before sending a new order.
- **BuyDatasetPath / SellDatasetPath** – binary files that store historical vectors.
- **NeighborCount** – number of neighbours used in the k-NN vote.
- **CandleType** – candle series used for all indicators.

## Usage recommendations
- Provide absolute or working-directory-relative paths to the dataset files before enabling classifier mode.
- Collect high-quality samples by running the strategy in training mode on historical data and exporting vectors manually.
- Optimise the thresholds and the neighbour count to adapt the classifier to new markets or instruments.
- Keep the instrument `Volume` parameter aligned with the risk model because the strategy always opens `Volume + |Position|` lots to reverse the net position when necessary.

## Differences from the MQL4 version
- The classifier datasets are only read; the original EA writes new samples during deinitialisation. Here the user must update the files manually after analysing the trade history.
- All protective orders are attached via StockSharp `StartProtection` instead of manual `OrderSend` parameters.
- Order closing in classifier mode always exits the entire position when `ClosePositionsOnSignal` is enabled, whereas the MQL4 script closed only profitable orders before taking new signals.
