# Pineconnector Strategy Template

This strategy demonstrates how to connect any indicator to generate trading signals. It uses two moving averages as an example and enters long when the fast average crosses above the slow average, and enters short on the opposite cross.

## Parameters
- **Fast Length** – period of the fast moving average.
- **Slow Length** – period of the slow moving average.
- **Candle Type** – candle type for calculation.
