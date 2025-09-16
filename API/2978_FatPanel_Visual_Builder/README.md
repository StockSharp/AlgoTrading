# FatPanel Visual Builder Strategy

The **FatPanel Visual Builder Strategy** is a StockSharp translation of the legacy FAT Panel Expert Advisor from MetaTrader. The original MQL implementation exposed a rich drag-and-drop canvas where users could link indicator, logic, state, and order blocks. This C# port keeps the modular philosophy but expresses every block connection through a single JSON document that the strategy reads at start-up.

## How the conversion works

* The MQL panel created buttons, tabs, and a timer-based dispatcher. Those UI concerns are removed entirely. Instead, the strategy parses the `Configuration` parameter (a JSON string) and instantiates the corresponding signal and logic blocks internally.
* Blocks are evaluated on every finished candle of the configured `CandleType`. Indicator blocks use StockSharp indicators (`SMA`, `EMA`, `SMMA`, `WMA`) and never rely on manual buffers.
* The original order blocks allowed symbol selection, stop-loss, and take-profit in “points”. In StockSharp the default security is taken from `Strategy.Security`; stop-loss and take-profit are reintroduced through the strategy parameters `StopLossPoints` and `TakeProfitPoints` and are converted to absolute price distances using `Security.PriceStep`.
* Time and day-of-week state filters mirror the MQL logic. The bid-price signal subscribes to Level1 data only if at least one rule requests it, replicating the on-demand update behavior of the panel dispatcher.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Data type and time-frame that feed every signal. |
| `Configuration` | JSON document describing rules, conditions, and actions. The default value reproduces the sample EMA/SMA cross strategy from the panel. |
| `Volume` | Default order size used by actions unless a rule overrides it. |
| `StopLossPoints` | Distance in price steps for the built-in risk protection. Set to `0` to disable the stop-loss. |
| `TakeProfitPoints` | Distance in price steps for the built-in take-profit. Set to `0` to disable. |

`StopLossPoints` and `TakeProfitPoints` are only activated when a positive value is supplied **and** the security exposes a valid `PriceStep`.

## Configuration structure

The JSON schema is designed to stay close to the FAT Panel block language:

```json
{
  "rules": [
    {
      "name": "Rule name (optional)",
      "all": [ /* conditions that must all be true */ ],
      "any": [ /* optional conditions, at least one must be true */ ],
      "none": [ /* optional conditions that must all be false */ ],
      "action": { "type": "Buy" | "SellShort" | "Close", "volume": 1.0 }
    }
  ]
}
```

Each condition item has a `type` field with one of the following values:

| Type | JSON fields | Purpose |
| --- | --- | --- |
| `comparison` | `operator`, `left`, `right`, `threshold` | Connects two signal blocks through logical operators (`Greater`, `Less`, `Equal`, `CrossAbove`, `CrossBelow`). Thresholds are interpreted as absolute price differences. Cross operators fire when the previous candle was on the opposite side and the current difference exceeds the threshold. |
| `position` | `required` | Mirrors the FAT panel state blocks (`Any`, `FlatOnly`, `FlatOrShort`, `FlatOrLong`, `LongOnly`, `ShortOnly`). |
| `time` | `start`, `end` | Intraday session filter in `HH:mm` format. Start > end keeps the overnight behavior of the MQL panel. |
| `dayOfWeek` | `days` | List of day names. When omitted the condition defaults to Monday–Friday, matching the panel defaults. |

Signals (`left` / `right`) are defined as:

```json
{ "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" }
{ "type": "Bid" }
{ "type": "Constant", "level": 1.2345 }
```

* `MovingAverage` supports `Simple`, `Exponential`, `Smoothed`, and `LinearWeighted` methods with any of the OHLC price sources. The indicator shares the strategy candle stream, just like the panel used chart-selected timeframes.
* `Bid` uses the latest best bid price from level1 updates (falls back to the candle close until a quote arrives).
* `Constant` reproduces the HLINE block and yields a static level.

Rule actions replicate the order blocks:

* `Buy` – opens or reverses to a long position when the current position is flat or short.
* `SellShort` – opens or reverses to a short position when the position is flat or long.
* `Close` – exits any open position using `ClosePosition()`.

A per-action `volume` can override the default `Volume` parameter.

## Execution flow

1. When the strategy starts it parses the configuration JSON. Invalid documents stop the strategy and emit an error log.
2. Indicators are instantiated and cached so that multiple rules can reuse the same signal definitions without duplicate calculations.
3. For every finished candle the strategy updates signal values and then evaluates each rule in order. `all` conditions must pass, `any` must pass at least once (if provided), and `none` must fail completely.
4. If an action is triggered the strategy logs the rule name and executes the requested market order.
5. Optional stop-loss and take-profit protections are armed once during `OnStarted` using the supplied point distances.

## Limitations and notes

* Only the primary `Strategy.Security` is supported. Cross-symbol routing from the original panel would require multiple strategy instances.
* The MQL dispatcher allowed deep nesting of logic blocks (e.g., AND inside OR). The JSON structure provides similar control through the `all`/`any`/`none` arrays, but extremely complex graphs may still need manual adaptation.
* The `Cross` operator uses the last candle only. The MQL block exposed a look-back buffer and delta in “points”; adapt the `threshold` field to emulate the required sensitivity.
* UI features such as drag positions, dialog windows, and toolbar icons have no direct equivalent in StockSharp and are intentionally omitted.

## Sample configuration

The default configuration embedded in the strategy is reproduced below for convenience:

```json
{
  "rules": [
    {
      "name": "EMA crosses above SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossAbove",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
        { "type": "time", "start": "09:00", "end": "17:00" },
        { "type": "position", "required": "FlatOrShort" }
      ],
      "action": { "type": "Buy" }
    },
    {
      "name": "EMA crosses below SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossBelow",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "position", "required": "LongOnly" }
      ],
      "action": { "type": "Close" }
    }
  ]
}
```

This sample mirrors the stock panel template: open a long on a 20/50 EMA-SMA bullish cross during the regular session and close the position on the inverse cross.
