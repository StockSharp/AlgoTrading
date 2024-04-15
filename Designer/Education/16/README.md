# Cube Converter: "Maximum Volume" Functionality

This schema showcases the functionality of the "Converter" cube with a focus on the "Maximum Volume" setting, integrated within a strategy that constructs candlestick data from tick data.

## Overview

The schema explains how to utilize the "Converter" cube to enhance trading strategies by identifying key moments based on volume data. The example strategy detailed here buys and sells based on specific candlestick patterns formed from tick data.

## Key Components

- **"Converter" Cube with "Maximum Volume"**: Explains how this cube can be used to extract maximum volume information from tick data, aiding in decision-making processes.
- **Candlestick Strategy**: Describes a strategy that relies on candlestick formations where decisions are based on the opening and closing prices of the candles.

## Detailed Breakdown

### Strategy Logic
- **Buying Condition**: The strategy initiates a buy order if the closing price of a candle is greater than its opening price, indicating a bullish sentiment.
- **Selling Condition**: Sells on the sixth candle regardless of the price movement, to capitalize on short-term gains or cut losses, showcasing a time-based exit strategy.

### Updates in Version 5
- **Flag Cube Modification**: The "Flag" cube and its triggering conditions have been revised to provide more precise and configurable signaling.
- **Formula Block Replacement**: All cubes from the formula block have been consolidated into a single "Formula" cube, simplifying the design and enhancing performance.

## Practical Application

- **Volume Analysis**: By employing the "Maximum Volume" converter, traders can pinpoint the highest volume levels within a given timeframe, which are often indicative of significant market interest or potential turning points.
- **Candlestick-Based Trading**: The strategy demonstrates how candlestick analysis, combined with volume data, can be used to make informed trading decisions, aligning with both trend-following and contrarian approaches.

## Conclusion

This schema not only illustrates the effective use of the "Converter" cube in a practical trading scenario but also highlights the enhancements brought by the latest version of the software, aiding users in adapting to updated features while optimizing their trading strategies.
