# Expert AML MFI Strategy

## Overview
The **Expert AML MFI Strategy** replicates the MetaTrader 5 expert advisor "Expert_AML_MFI" using the StockSharp high-level API. It focuses on the *Meeting Lines* candlestick pattern and validates every signal with the **Money Flow Index (MFI)** oscillator. The strategy automatically maintains the necessary candle statistics, identifies bullish or bearish reversals, and manages open positions whenever the MFI crosses oversold or overbought thresholds.

## Trading Logic
1. **Candle preparation** – the strategy subscribes to the selected timeframe (H1 by default) and keeps the last two completed candles together with the moving average of candle bodies. The average body size is calculated through a `SimpleMovingAverage` applied to the absolute candle body size, mirroring the MT5 implementation.
2. **Pattern detection** – two specialized helpers recognise *Bullish Meeting Lines* and *Bearish Meeting Lines*:
   - Bullish setup: a long bearish candle followed by a long bullish candle that closes near the previous close (within 10% of the average body).
   - Bearish setup: a long bullish candle followed by a long bearish candle with similar closing prices.
3. **MFI confirmation** – the previous MFI value must be below the bullish entry level (default 40) for long trades or above the bearish entry level (default 60) for short trades.
4. **Position management** – the last two MFI readings are tracked to detect crossings of the oversold (30) and overbought (70) levels:
   - A cross above either level exits short positions.
   - A cross below the oversold level or above the overbought level exits long positions.
5. **Order execution** – when a valid pattern and MFI confirmation occur, the strategy closes any opposite exposure and opens a new position at market with the configured base volume.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe used for candle subscription. | 1 hour time frame |
| `MfiPeriod` | Number of bars for the MFI oscillator. | 12 |
| `BodyAveragePeriod` | Window length for the average body size calculation. | 4 |
| `BullishEntryLevel` | Maximum MFI value allowed for bullish entries. | 40 |
| `BearishEntryLevel` | Minimum MFI value required for bearish entries. | 60 |
| `OversoldLevel` | Oversold level used for exit signals. | 30 |
| `OverboughtLevel` | Overbought level used for exit signals. | 70 |
| `TradeVolume` | Base order volume applied to new trades. | 1 |

All parameters can be optimised directly inside StockSharp Designer thanks to the `StrategyParam` wrappers.

## Indicators and Visuals
- **Money Flow Index** – bound to the candle subscription for confirmation and displayed on the chart when a chart area is available.
- **Simple Moving Average of candle bodies** – internal use only, reproducing the MT5 average body calculation.

## Notes
- The strategy calls `StartProtection()` once to enable built-in position protection facilities.
- Trade commands use `BuyMarket` and `SellMarket` helpers to flatten the current position before opening a new one, matching the MetaTrader expert advisor behaviour.
- No Python port is provided in accordance with the project requirements.
