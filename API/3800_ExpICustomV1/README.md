# ExpICustomV1 Strategy

## Summary

The **ExpICustomV1 Strategy** is a StockSharp port of the MetaTrader expert `exp_iCustom_v1`. The strategy reads trade signals from a configurable indicator instance and reacts to non-zero values in the selected buffers. When the buy buffer is non-zero the strategy opens a long position, while the sell buffer triggers a short entry. Protective stop-loss, take-profit, trailing and break-even logic reproduce the money-management options of the original expert.

> **Note:** Only the C# implementation is provided. A Python version is not available yet.

## Trading Logic

1. Subscribe to the primary timeframe specified by **Candle Type** and process closed candles only.
2. Instantiate the indicator defined by **Indicator Name** and apply the slash-separated **Indicator Parameters** (supports both `Name=Value` pairs and ordered numeric values).
3. Store final indicator outputs in a history buffer so that any shift can be accessed on later candles.
4. When the buy buffer value at **Indicator Shift** is not zero the strategy opens/maintains a long position. When the sell buffer is non-zero the strategy opens/maintains a short position.
5. If both buffers return non-zero values simultaneously the signals cancel each other to avoid ambiguous entries.
6. Optional **Close On Reverse** exits the current position before reacting to the opposite signal.
7. Sleep logic enforces a minimum number of bars between consecutive entries in the same direction. The timer can be cancelled when the opposite signal fires if **Cancel Sleeping** is enabled.
8. Positions are protected by stop-loss, take-profit, optional trailing stop and break-even locking. All distances are expressed in price points.

## Indicator configuration

* **Indicator Name** – Full type name or short StockSharp indicator name (for example `SMA`, `MACD`, `BollingerBands`).
* **Indicator Parameters** – Slash-separated list applied to the indicator. Both `Length=14/Width=2` and ordered values like `14/2/0.7` are supported.
* **Override blocks** – Up to five replacements let you adjust parameter values by index during optimisation, similar to the `Opt_X` inputs in the original expert. Indexes are zero-based.

## Risk & money management

* **Base Volume** – Amount sent with each market order.
* **Stop Loss / Take Profit** – Absolute distances in points from the entry price.
* **Trailing Stop** – Activates after the specified profit and maintains the configured distance from the extreme price.
* **Break Even** – Moves the stop towards the entry price after the specified profit and optionally locks additional points.

## Parameter reference

| Parameter | Description |
|-----------|-------------|
| Candle Type | Timeframe used for the indicator and signal evaluation. |
| Indicator Name | Type name of the indicator instance. |
| Indicator Parameters | Slash-separated list of indicator parameters. |
| Buy Buffer / Sell Buffer | Buffer indexes that contain the buy/sell markers. |
| Indicator Shift | Historical shift applied when reading indicator buffers. |
| Override blocks | Replace specific parameter positions during runtime. |
| Sleep Bars | Minimum bars between entries in the same direction. |
| Cancel Sleeping | Reset the sleep timer after an opposite signal. |
| Close On Reverse | Close existing position when the opposite signal appears. |
| Max Orders / Max Buy / Max Sell | Soft caps that limit the number of simultaneous positions. |
| Stop Loss / Take Profit | Distance in points for protective orders. |
| Trailing Stop settings | Parameters controlling the trailing stop activation and distance. |
| Break Even settings | Parameters controlling break-even activation and lock distance. |
| Base Volume | Volume of each market entry. |

## Usage

1. Add the strategy to your strategy container and set the **Security** and **Portfolio**.
2. Configure **Indicator Name** and **Indicator Parameters** to match the target custom indicator.
3. Adjust risk settings (stop, take, trailing, break even) and the base order volume.
4. Run the strategy. It will subscribe to the chosen timeframe, evaluate the indicator buffers and send market orders when conditions are met.

## Limitations

* The indicator must be available as a StockSharp indicator type. Binary MetaTrader indicators cannot be loaded directly.
* Money-management modes that depend on broker-side free margin are simplified to a fixed base volume.
