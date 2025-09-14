# Color Schaff WPR Trend Cycle Strategy

This strategy implements the **Color Schaff WPR Trend Cycle** expert from MetaTrader.
It uses the Schaff Trend Cycle calculated from fast and slow Williams %R values to detect market turns.

The algorithm works on finished candles only. When the indicator value crosses above the upper level, the strategy opens a long position and closes any existing short position. When the value crosses below the lower level, it opens a short position and closes any existing long position.

## Parameters
- **Fast WPR** – period for the fast Williams %R calculation.
- **Slow WPR** – period for the slow Williams %R calculation.
- **Cycle** – cycle length used in the Schaff Trend calculation.
- **High Level** – upper trigger level for long entries.
- **Low Level** – lower trigger level for short entries.
- **Candle Type** – timeframe of candles for indicator evaluation.

## Links
- Original MQL source: `MQL/13489/mql5/Experts/exp_colorschaffwprtrendcycle.mq5`
- Indicator: `MQL/13489/mql5/Indicators/colorschaffwprtrendcycle.mq5`
