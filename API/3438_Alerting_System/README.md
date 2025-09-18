# Alerting System Strategy

The **Alerting System Strategy** is a StockSharp port of the MetaTrader 5 expert advisor "AlertingSystem" (MQL folder `31843`). The original EA draws two horizontal lines and plays a sound whenever the bid trades above the upper line or the ask trades below the lower line. This C# conversion keeps the alerting behavior while using StockSharp's high-level API for data access and notification logging.

## Core Idea

* Listen to real-time Level 1 market data (best bid and best ask).
* Trigger one-shot alerts when the bid is greater than or equal to a configurable upper threshold.
* Trigger one-shot alerts when the ask is less than or equal to a configurable lower threshold.
* Reset the alert flags when prices move back inside the band so the next breakout can be detected.

Unlike the MQL implementation that repeatedly plays a sound on every tick, the StockSharp version sends a single informational log entry for each breakout event. This avoids log flooding while still notifying the operator when price targets are reached.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `UpperPrice` | Bid level that activates the bullish alert. Set to `0` to disable. | `0` |
| `LowerPrice` | Ask level that activates the bearish alert. Set to `0` to disable. | `0` |

Both parameters are standard `StrategyParam<decimal>` values that can be optimized or adjusted at runtime. You can move the thresholds around during live trading just as you would reposition the horizontal lines in MetaTrader.

## Data Subscriptions and Workflow

1. When the strategy starts it subscribes to Level 1 data via `SubscribeLevel1().Bind(ProcessLevel1).Start()`.
2. Incoming `Level1ChangeMessage` objects update cached best bid and best ask values.
3. Each update calls the alert checks:
   * **Upper alert** – fires once when `BestBid >= UpperPrice` and the price was previously below the level.
   * **Lower alert** – fires once when `BestAsk <= LowerPrice` and the price was previously above the level.
4. Alert resets occur automatically when the market trades back within the corridor.

## Logging and Notifications

Alerts are written with `AddInfoLog` and include the current bid/ask values and the configured levels. Integrate your own notification pipeline (emails, messengers, custom sounds) by overriding `OnInfo` or subscribing to the strategy's log events in your hosting application.

## Usage Tips

* Set only the thresholds you care about – the other can remain `0` to stay disabled.
* Combine the strategy with other modules that react to `Info` logs if you want to reproduce audible or push notifications.
* Because the strategy never places orders, there is no need to call `StartProtection()`.

## Differences from the Original EA

* The StockSharp version uses Level 1 data instead of creating chart objects.
* Alerts are one-shot per breakout to keep the log clean.
* Everything else (parameters, logic thresholds, conditions) matches the MQL reference.

## Files

* `CS/AlertingSystemStrategy.cs` – C# strategy implementation.
* `README.md` – English documentation (this file).
* `README_ru.md` – Russian translation with additional explanation.
* `README_cn.md` – Simplified Chinese translation.
