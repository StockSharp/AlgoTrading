# Tuyul Gap End Of Week

## Overview
Tuyul Gap End Of Week ports the MetaTrader 5 expert advisor `TuyulGAP` to StockSharp. The strategy prepares for the weekly market open by scanning a configurable number of recent candles on Friday night, placing a pair of breakout stop orders around the highest high and lowest low. Only one trading session per week is allowed; once the orders are staged the strategy waits for price to gap through either level. Any open position that reaches a secure profit target in account currency is closed immediately, and all remaining pending orders are cancelled on Monday to reset the workflow for the next week.

## Strategy logic
* **Weekly session trigger** – the setup executes on a configurable weekday (Friday by default) when the exchange clock reaches the configured hour. During the minute window (23:00–23:15 by default) the strategy prepares the breakout levels once per session.
* **Dynamic breakout levels** – the highest high and lowest low of the previous `Lookback Bars` finished candles define the trigger prices. Buy Stop is placed one tick above the high, Sell Stop one tick below the low, mimicking the MetaTrader point offset.
* **Pending-order hygiene** – if a stop order already exists for the week it is not recreated. The opposite pending order remains active after one side is triggered, so the strategy can trade either direction of the gap.
* **Secure profit exit** – open positions are monitored on every finished candle. When unrealized profit for a position reaches the secure profit target (in the portfolio currency) it is flattened at market regardless of direction.
* **Weekly reset** – at the first Monday candle the strategy cancels any still-active pending orders and re-arms the session flag so the next Friday setup can be staged.

## Parameters
* **Volume** – order volume for the breakout stop orders.
* **Stop Loss (points)** – distance from the entry price, expressed in instrument points, used to place a protective stop after a position opens. Set to `0` to disable the stop.
* **Lookback Bars** – number of finished candles inspected to compute the weekly high and low levels.
* **Setup Day Of Week** – day index (0=Sunday … 6=Saturday) that triggers the weekly setup. The default value of `5` keeps the original Friday behavior.
* **Setup Hour** – exchange hour used as the anchor for staging the breakout orders.
* **Setup Minute Window** – number of minutes after `Setup Hour` when the setup remains valid. With the default value `15` the strategy runs between 23:00 and 23:15 inclusive.
* **Secure Profit Target** – minimum unrealized profit per position (in portfolio currency) that triggers an immediate market exit.
* **Candle Type** – timeframe used for the high/low scan and the monitoring loop.

## Additional notes
* The stop-loss order is submitted only after a position opens, because StockSharp does not support attaching a protective stop directly to a pending stop order.
* Volume, price, and stop levels are normalized using the security’s step and precision information that StockSharp provides.
* There is no Python translation for this strategy; only the C# implementation is included in this package.
