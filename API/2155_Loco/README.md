# Loco Strategy

This strategy implements the "Loco" indicator originally written in MQL5. The indicator analyzes candle prices and assigns a color (green or magenta). A change in color signals a trend reversal.

## Logic
- The indicator computes a series using a configurable price (close by default) and a lookback length.
- When the color switches from magenta to green, the strategy closes any short position and opens a long position.
- When the color switches from green to magenta, the strategy closes any long position and opens a short position.

## Parameters
- **Candle Type** – type of candles used in the strategy.
- **Length** – number of bars for comparing price.
- **Price Type** – price used in indicator calculation.

## Notes
The strategy uses a custom implementation of the Loco indicator. Python version is not provided.
