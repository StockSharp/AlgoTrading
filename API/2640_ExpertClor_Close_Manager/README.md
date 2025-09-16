# ExpertClor Close Manager Strategy

## Overview

ExpertClor Close Manager Strategy is a risk management module that supervises existing positions and closes them when exit conditions are met. The logic is converted from the original MetaTrader Expert Advisor *ExpertClor_v01* that only manages exits without placing new orders. The StockSharp port keeps the same behaviour: the strategy never opens trades on its own, it just monitors the active position and issues market exit orders.

## Core Logic

1. **Moving Average Cross Exit**  
   Two configurable moving averages (fast and slow) are evaluated on every finished candle. When the fast average crosses below the slow one, long positions are closed; when the fast average crosses above the slow, short positions are closed. The check uses the last two completed candles to mirror the original MQL5 implementation.

2. **ATR-Based Trailing Stop**  
   The strategy calculates an Average True Range with adjustable period and multiplier. For long trades the trailing stop is set to the latest close minus `ATR × multiplier`; for shorts it is the latest close plus `ATR × multiplier`. Stops only tighten in the profitable direction, replicating the StopATR_auto indicator used in MQL5.

3. **Breakeven Transfer**  
   After the price moves by the configured number of pips in favour of the trade, the protective stop is shifted to the entry price (breakeven). This mechanism is applied separately for long and short positions and can work together with the ATR trailing stop.

4. **Position Awareness**  
   All calculations are performed on the selected candle series. When no position is open the internal stop levels are cleared. Every forced exit resets the trailing state to avoid stale levels once a new position appears.

## Indicators

- Fast moving average with selectable type (SMA, EMA, SMMA, WMA) and applied price.
- Slow moving average with the same configuration options.
- Average True Range (ATR) for volatility-based trailing.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `MaCloseEnabled` | Enable/disable the moving average cross exit. | `true` |
| `AtrCloseEnabled` | Enable/disable the ATR trailing exit. | `true` |
| `FastMaPeriod` | Length of the fast moving average. | `5` |
| `FastMaMethod` | Type of the fast moving average (Simple, Exponential, Smoothed, Weighted). | `Exponential` |
| `FastPriceType` | Applied price for the fast MA (Close, Open, High, Low, Typical, Median, Weighted). | `Close` |
| `SlowMaPeriod` | Length of the slow moving average. | `7` |
| `SlowMaMethod` | Type of the slow moving average. | `Exponential` |
| `SlowPriceType` | Applied price for the slow MA. | `Open` |
| `BreakevenPips` | Distance in pips required to move the stop to breakeven. | `0` |
| `AtrPeriod` | ATR averaging period. | `12` |
| `AtrTarget` | Multiplier applied to the ATR value when computing the trailing stop. | `2.0` |
| `CandleType` | Candle series used for all calculations. | `5m time frame` |

## Usage Notes

- Attach the strategy to a security where an external component opens and maintains trades. ExpertClor Close Manager will only issue market exits.
- The strategy requires finished candles; make sure the candle subscription corresponds to the timeframe used by the original Expert Advisor.
- Breakeven logic uses the instrument `PriceStep` to convert pips to price units. Set `BreakevenPips` to zero to disable the transfer.
- ATR trailing only starts once the ATR indicator is formed. Until then only the moving average exit (if enabled) can trigger.
- Because exits are sent as market orders, slippage protection should be handled at the connector or broker level if needed.

## Conversion Details

- Original StopATR_auto custom indicator is emulated with the built-in Average True Range trailing logic.
- MQL5 loop through positions is replaced with StockSharp high-level APIs and candle subscriptions.
- English in-code comments explain each logical block for future maintenance.
