# Morning/Evening Star with MFI Confirmation Strategy

## Overview
This strategy replicates the logic of the MetaTrader expert `Expert_AMS_ES_MFI`, combining multi-candle reversal patterns with momentum confirmation from the Money Flow Index (MFI). It monitors three-candle Morning Star and Evening Star formations on the selected timeframe and filters the signals using MFI thresholds to confirm exhaustion of the current swing before entering trades. Momentum reversals detected by MFI crossings are also used to close open positions.

## Trading Logic
- **Data Source**: Finished candles of the configured timeframe and their associated MFI values.
- **Indicators**:
  - Money Flow Index (MFI) – period is configurable (default 49).
- **Entry Rules**:
  - **Long**: Detect a Morning Star pattern (strong bearish candle, small-bodied middle candle, strong bullish candle closing above the midpoint of the first) and require the previous candle's MFI to be below the bullish confirmation threshold (default 40).
  - **Short**: Detect an Evening Star pattern (strong bullish candle, small-bodied middle candle, strong bearish candle closing below the midpoint of the first) and require the previous candle's MFI to be above the bearish confirmation threshold (default 60).
  - When flipping positions, the strategy first closes the opposite exposure before opening the new trade.
- **Exit Rules**:
  - **Long Exit**: Close the position when the MFI crosses above the upper exit level (default 70) or drops below the lower exit level (default 30), signalling either overbought momentum or a failed reversal.
  - **Short Exit**: Close the position when the MFI crosses above the lower exit level (default 30) or above the upper exit level (default 70), signalling growing bullish momentum.
- **Order Type**: Market orders using the strategy volume configured in the StockSharp environment.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Timeframe of the candles used for analysis. | 1-hour candles |
| `MfiPeriod` | Period of the MFI indicator. | 49 |
| `BullishMfiThreshold` | MFI level that confirms Morning Star signals. | 40 |
| `BearishMfiThreshold` | MFI level that confirms Evening Star signals. | 60 |
| `UpperExitLevel` | MFI level used for overbought exit detection. | 70 |
| `LowerExitLevel` | MFI level used for oversold exit detection. | 30 |

All parameters can be optimised inside StockSharp Designer/Optimizer.

## Usage Notes
1. Attach the strategy to the desired security and set the `CandleType` to match the chart timeframe from the original MQL expert.
2. Configure the risk parameters, such as strategy volume or broker-specific order size, via the StockSharp platform.
3. Enable the strategy. It will automatically subscribe to candles, calculate MFI values, and manage positions according to the rules above.

## Origin
The strategy is a direct conversion of the MQL5 expert advisor located in `MQL/323`, preserving its pattern and MFI-based decision logic while adapting it to the StockSharp high-level API.
