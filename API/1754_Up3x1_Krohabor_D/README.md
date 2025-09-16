# Up3x1 Krohabor D Strategy

## Overview
This strategy uses three simple moving averages (fast, middle, slow) to identify trend direction. A long position is opened when the fast MA crosses above the middle MA and both fast and middle MAs are above the slow MA on the current and previous bars. A short position is opened when the fast MA crosses below the middle MA and both fast and middle MAs are below the slow MA on the current and previous bars.

Positions are protected with take profit, stop loss and optional trailing stop levels. Orders are executed at market prices.

## Parameters
- **Volume** – order size.
- **Fast Period** – period of the fast SMA.
- **Middle Period** – period of the middle SMA.
- **Slow Period** – period of the slow SMA.
- **Take Profit** – distance to the profit target in price units.
- **Stop Loss** – distance to the protective stop in price units.
- **Trailing Stop** – distance for trailing stop activation in price units.
- **Candle Type** – timeframe of the candles used for calculations.

## Signals
- **Buy** – fast MA crosses above middle MA and both fast and middle MAs stay above the slow MA.
- **Sell** – fast MA crosses below middle MA and both fast and middle MAs stay below the slow MA.

## Protections
- Take profit and stop loss levels are set on entry.
- When enabled, trailing stop moves the protective stop in the trade direction as price advances.

## Notes
This is a direct conversion of the original MQL strategy into StockSharp using the high level API and built‑in indicators.
