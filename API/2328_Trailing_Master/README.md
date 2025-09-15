# Trailing Master

## Overview
Trailing Master implements a basic trailing stop mechanism that continuously moves a protective stop order as a position becomes profitable. The strategy is a C# port of the MetaTrader script "TrailingMaster".

The strategy monitors tick data and tracks the best price achieved since entering the position. Once the current profit exceeds the configured offset, a stop order is placed or shifted so that it trails the price by the specified number of ticks. When the market reverses to the trailing stop, the position is closed automatically.

## Parameters
- `TrailingStop` – distance from the current price to the stop in ticks.
- `UseComment` – attach a custom comment to the stop order when enabled.
- `Comment` – text used for the comment when `UseComment` is true.
- `UseMagicNumber` – attach a custom identifier to the stop order when enabled.
- `MagicNumber` – identifier used when `UseMagicNumber` is true.

## Usage
1. Configure the parameters according to your risk preferences.
2. Start the strategy with an existing position or allow another module to open one.
3. The strategy begins trailing once the profit exceeds `TrailingStop` ticks.
4. The stop order is shifted only in the direction of profit and never widened.

## Notes
- The strategy does not generate entry signals; it only manages trailing stops for an existing position.
- Comment and magic number parameters are provided for compatibility but may depend on broker support.
