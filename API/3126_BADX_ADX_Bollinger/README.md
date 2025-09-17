# BADX ADX Bollinger Strategy

## Overview

This strategy reproduces the MetaTrader BADX expert advisor using the StockSharp high-level API. It combines the **Average Directional Index (ADX)** with **Bollinger Bands** to trade range-bound conditions: when ADX falls below a configurable threshold and price touches the outer band, the strategy fades the move expecting a mean reversion. All protective orders, including stop-loss, take-profit, and optional trailing stop, are managed automatically by `StartProtection`.

## How It Works

1. Subscribes to the configured candle series and feeds both an `AverageDirectionalIndex` and a `BollingerBands` indicator through high-level bindings.
2. For every finished candle the callback receives the ADX value as well as the upper and lower Bollinger envelopes.
3. If ADX is below `AdxLevel`, the market is considered non-trending:
   - When the close price is below the lower band and there is no open position, the strategy buys at market.
   - When the close price is above the upper band and there is no open position, the strategy sells at market.
4. Risk management converts pip distances into absolute price offsets. Stop-loss, take-profit, and trailing parameters (if enabled) are applied immediately after entries via the protection manager.
5. Only one position can be active at a time. Exits occur through protective orders or trailing stop adjustments.

## Parameters

- **CandleType** (`DataType`): Timeframe used for the indicator calculations. Default is 15-minute candles.
- **AdxPeriod** (`int`): Averaging period for the ADX indicator. Default is 30.
- **AdxLevel** (`decimal`): Maximum ADX value that still qualifies as a ranging market. Default is 20.
- **BollingerPeriod** (`int`): Period for the Bollinger Bands moving average. Default is 10.
- **BollingerDeviation** (`decimal`): Standard deviation multiplier for the Bollinger Bands. Default is 1.5.
- **StopLossPips** (`decimal`): Stop-loss distance measured in pips. Default is 50.
- **TakeProfitPips** (`decimal`): Take-profit distance measured in pips. Default is 50.
- **TrailingStopPips** (`decimal`): Trailing stop distance in pips. Default is 5.
- **TrailingStepPips** (`decimal`): Minimal price improvement in pips before the trailing stop is adjusted. Default is 5.

## Usage

1. Attach the strategy to a security and configure the parameters as desired.
2. Start the strategy. It will automatically subscribe to the required candle stream, build the indicators, and set up protective orders.
3. Monitor trades on the chart area: candles, the Bollinger Bands, and executed orders are visualized when the platform supports charting.
4. Adjust risk parameters (stop-loss, take-profit, trailing distances) to match the instrument volatility or personal preferences.

## Notes

- Only finished candles are processed to avoid premature entries.
- Pip size is derived from the security’s `PriceStep`; when the instrument uses 3 or 5 decimal digits the pip is adjusted by a factor of ten, mimicking the original expert advisor.
- The strategy keeps `Volume` at `1` by default. Modify the base class `Volume` property to match preferred trade size.
- All inline comments in the source code are written in English in accordance with repository guidelines.
