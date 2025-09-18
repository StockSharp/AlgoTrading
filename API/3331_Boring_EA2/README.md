# Boring EA2 Alert

## Overview
Boring EA2 Alert recreates the notification logic of the MetaTrader 4 expert advisor `boring-ea2`. The strategy listens to finished candles, calculates three simple moving averages (SMA 3, SMA 20, SMA 150), and emits informative logs whenever a crossover happens between the moving averages. The implementation intentionally avoids order placement: the goal is to provide traders with timely alerts that they can combine with their discretionary execution or other automated strategies.

## Strategy logic
### Moving average tracking
* **Short-term bias** – a 3-period SMA reacts to immediate price changes.
* **Medium trend** – a 20-period SMA smooths price over the short-term swing horizon.
* **Long trend** – a 150-period SMA represents the dominant backdrop trend.

### Crossover detection
* **SMA3 vs SMA20** – reports "crossed up" when SMA3 rises above SMA20 and "crossed down" when it falls below. Internal flags guarantee that each transition is reported once.
* **SMA3 vs SMA150** – mirrors the same logic against the long-term average to detect momentum surges or reversals against the prevailing trend.
* **SMA20 vs SMA150** – adds a medium/long-term confirmation layer so that shifts in the higher timeframe structure trigger their own alerts.
* **Initialization guard** – the first finished candle only seeds the initial state. Alerts begin with the second finished candle once a true change in relationship is observed.

### Notification format
* Alerts mirror the original EA message: `Alert!!! - SYMBOL - TF - description`.
* The timeframe code is derived from the configured candle type. Standard MetaTrader-style labels (M1, M5, H1, etc.) are used when available; other timeframes fall back to a compact notation (for example, `M45` or `D2`).
* Messages are written with `AddInfoLog`, enabling routing to log viewers, scripts, or GUI dashboards.

## Parameters
* **Short SMA Length** – number of periods for the fast moving average (default `3`).
* **Medium SMA Length** – number of periods for the intermediate moving average (default `20`).
* **Long SMA Length** – number of periods for the slow moving average (default `150`).
* **Candle Type** – timeframe used to calculate the moving averages. The default is 1-minute candles, matching the EA's tick-based checks with high reactivity.

## Additional notes
* The strategy does not submit, modify, or cancel orders. It is purely informational.
* Because `Bind` feeds finalized values, each crossover is evaluated on completed candles. This avoids the noisy intra-bar flips that the original EA mitigated by counting ticks.
* The logging-based notifications can be integrated with custom handlers by subscribing to strategy log events within a hosting application.
* No Python translation is provided at this time; only the C# version is included in the API package.
