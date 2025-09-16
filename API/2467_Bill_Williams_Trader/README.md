# Bill Williams Trader Strategy

This strategy implements a simplified version of Bill Williams' trading approach based on the **Alligator** indicator and **Fractals**.

## How it works

- Calculates Alligator lines using Smoothed Moving Averages (SMMA):
  - **Jaw** length (default 13)
  - **Teeth** length (default 8)
  - **Lips** length (default 5)
- Detects bullish and bearish fractals on completed candles.
- **Buy** when price breaks above the last upper fractal that is above the Alligator's teeth line.
- **Sell** when price breaks below the last lower fractal that is below the Alligator's teeth line.
- **Exit** long positions when the close price drops below the lips line.
- **Exit** short positions when the close price rises above the lips line.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `JawLength` | Period of the Alligator jaw SMMA | 13 |
| `TeethLength` | Period of the Alligator teeth SMMA | 8 |
| `LipsLength` | Period of the Alligator lips SMMA | 5 |
| `CandleType` | Candle type used for calculations | 15-minute candles |

All parameters can be optimized through the strategy parameter interface.

## Usage

1. Compile the solution:
   ```bash
   dotnet build
   ```
2. Launch the strategy within the StockSharp environment and select the desired security and timeframe.

## Notes

This example demonstrates the high-level API usage with indicator bindings and does not implement position sizing or risk management beyond simple exits.
