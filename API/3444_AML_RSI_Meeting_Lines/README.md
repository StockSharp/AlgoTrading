# AML RSI Meeting Lines Strategy

## Overview
The **AML RSI Meeting Lines Strategy** is a StockSharp port of the MetaTrader 5 expert advisor `Expert_AML_RSI.mq5`. The original system combines Japanese candlestick pattern recognition with the Relative Strength Index (RSI) to trade bullish and bearish "Meeting Lines" reversals. This conversion keeps the core trading logic while adapting it to StockSharp's high-level API with candle subscriptions and built-in indicators.

## Trading Logic
- Subscribes to a configurable candle type and processes only finished candles.
- Calculates a Simple Moving Average of candle body sizes to detect "long" candles that form Meeting Lines patterns.
- Tracks RSI values on the two most recent completed candles for confirmation and exit signals.
- **Bullish setup**: two-bar Meeting Lines reversal with RSI below the bullish threshold triggers long entries.
- **Bearish setup**: mirrored pattern with RSI above the bearish threshold triggers short entries.
- **Position exits**: RSI crossovers through configurable lower and upper levels close open trades in the opposite direction.
- Uses `BuyMarket`, `SellMarket`, and `ClosePosition` helpers to manage exposure and automatically flips position size when a contrary signal appears.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe used to evaluate candlestick patterns. | 1 hour time frame |
| `RsiPeriod` | RSI lookback length. | 11 |
| `BodyAveragePeriod` | Number of candles for the average body size. | 3 |
| `BullishRsiLevel` | Maximum RSI that validates bullish Meeting Lines. | 40 |
| `BearishRsiLevel` | Minimum RSI that validates bearish Meeting Lines. | 60 |
| `LowerExitLevel` | RSI level that closes shorts on upward crosses. | 30 |
| `UpperExitLevel` | RSI level that closes longs on downward crosses. | 70 |

All parameters are exposed as `StrategyParam<T>` objects so they can be optimized in the StockSharp designer.

## Risk Management
- `StartProtection()` is invoked in `OnStarted` to enable the framework's built-in position monitoring.
- The strategy closes existing exposure whenever RSI crosses the configured exit boundaries before considering new signals.
- Market orders automatically reverse the position by adding the absolute value of the current exposure to the configured volume.

## Conversion Notes
- Candlestick averaging uses `SimpleMovingAverage` fed with absolute candle bodies, mirroring the `AvgBody` helper from the MQL5 source.
- RSI confirmation relies on the values from the two previous candles, reproducing the `RSI(1)` and `RSI(2)` checks from the original expert.
- All comments in the code were rewritten in English and the structure follows the repository requirement of file-scoped namespaces with tab indentation.

## Usage
1. Attach the strategy to a security in StockSharp and select the desired candle type.
2. Configure RSI and exit thresholds to match the trading venue or instrument volatility.
3. Run the strategy in paper trading first to validate pattern recognition before moving to live trading or optimization.
4. Use the provided parameters during optimization to fine-tune RSI levels and average body length for different markets.

## Disclaimer
This strategy is provided for educational purposes only. Test thoroughly on historical data and in simulated environments before deploying it on live capital.
