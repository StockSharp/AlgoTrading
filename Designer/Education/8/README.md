# Advanced Timeframe Strategy Schema

This file illustrates a complex strategy schema using candles from different timeframes, specifically designed with the Designer platform from StockSharp. This example utilizes varying conditions across multiple branches to execute trades based on historical price data.

## Strategy Details

The schema is divided into two main branches, each using five-minute candles compared against historical price extremes to make trading decisions:

### First Branch - Historical Extremes
- **Buy Condition**: The strategy initiates a buy order if the closing price of a five-minute candle is greater than the highest price of the last 20 days.
- **Sell Condition**: A sell order is executed if the closing price of a five-minute candle is less than the lowest price of the last 10 days.

### Second Branch - Reverse Conditions
- **Sell Condition**: Executes a sell order if the closing price of a five-minute candle is less than the lowest price of the last 20 days.
- **Buy Condition**: Initiates a buy if the closing price of a five-minute candle is greater than the highest price of the last 10 days.

## Version-Specific Features and Changes
- **Flag Cube Appearance**: In Designer version 5, the appearance of the flag cube has been updated.
- **Strategy Adaptations**: Also in version 5, the strategy has been modified to include two cubes for both sell and buy signals. This adjustment is due to a change in how signals trigger the actions in the newer version of the Designer.

This schema provides a framework for implementing and testing strategies that react to significant price movements by comparing short-term price actions against long-term price records. The multi-branch approach allows traders to experiment with different strategic responses based on the same underlying data.