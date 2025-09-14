# Indicator Buffers Strategy

This strategy demonstrates how to inspect indicator buffers using the StockSharp high-level API.

The original MQL expert advisor `indicator_buffers.mq4` reads up to eight buffers from a custom indicator and prints their values as text labels on the MetaTrader chart. The goal of this port is to show how a similar diagnostic tool can be built in StockSharp.

The strategy subscribes to candle data and processes the **Bollinger Bands** indicator. On each finished candle it logs the values of the middle, upper and lower bands as Buffer0, Buffer1 and Buffer2. The remaining buffers (Buffer3–Buffer7) are reserved for indicators with more components and are reported as not used.

This implementation is meant for educational purposes and does not place any orders.

## Parameters

- **Candle Type** – type of candles used for calculations.
- **Bands Period** – number of samples in the Bollinger Bands moving average.
- **Bands Width** – width multiplier for Bollinger Bands.

## Usage

1. Add the strategy to your StockSharp environment.
2. Configure the parameters if necessary.
3. Start the strategy. The log will display buffer values for every completed candle.

## Notes

- The example uses Bollinger Bands because it exposes multiple output buffers.
- Additional indicator buffers can be added by extending the processing method.
