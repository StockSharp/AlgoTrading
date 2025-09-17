[Русский](README_ru.md) | [中文](README_cn.md)

The **Alerting System** strategy is a faithful StockSharp conversion of the MetaTrader 4 expert advisor `AlertingSystem.mq4`. The original script draws two horizontal lines and plays a sound whenever the market touches them. The StockSharp version accomplishes the same goal by subscribing to Level1 (best bid/ask) quotes and printing journal messages when either configurable alert level is crossed.

## Core Idea

1. Register a Level1 data stream so the strategy receives tick-by-tick bid and ask updates, mirroring the MQL `OnTick` handler.
2. Read the user-defined `UpperPrice` and `LowerPrice` levels. A value of `0` disables the corresponding alert, just like removing the horizontal line in MetaTrader.
3. Compare every incoming bid with the upper level and every ask with the lower level.
4. Emit a single log notification when the price crosses an active level and wait until the market returns to the safe zone before arming the alert again. This prevents noisy duplicate alerts while preserving the intent of the original sound trigger.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `UpperPrice` | `0` | Upper horizontal alert level. Set to `0` to disable the check. |
| `LowerPrice` | `0` | Lower horizontal alert level. Set to `0` to disable the check. |

Both parameters are exposed through the Designer UI. They can be changed before the launch or while the strategy is running; the next quote update will use the new levels.

## Runtime Behavior

- **Data subscription**: `GetWorkingSecurities` requests Level1 data, ensuring the strategy receives bid/ask updates even without candles or trades.
- **Initialization**: When `OnStarted` fires the strategy logs the currently configured levels so the operator can verify the setup.
- **Alert detection**: Helper methods (`CheckUpperAlert` and `CheckLowerAlert`) store internal flags to guarantee that each breach produces exactly one notification until the market moves back beyond the threshold.
- **No trading**: The conversion does not send orders. It is purely an alerting utility, matching the behavior of the MetaTrader script that only played a sound.
- **Reset handling**: `OnReseted` clears the internal flags so the next run starts with fresh alert states.

## Typical Usage Steps

1. Select the desired instrument in StockSharp Designer and attach `AlertingSystemStrategy`.
2. Specify the upper and/or lower alert levels. Leave a value at `0` to ignore that side.
3. Start the strategy. The log will display entries confirming which alerts are active.
4. Monitor the journal window. When the bid rises above the upper level or the ask falls below the lower level, the strategy records a descriptive message.

## Conversion Notes

- The original MetaTrader advisor created two draggable horizontal lines. StockSharp uses numeric parameters instead, which keeps the workflow deterministic and more suitable for algorithmic execution.
- MetaTrader triggered the `PlaySound` function on every qualifying tick. To avoid overwhelming the log, the conversion debounces alerts until the price re-enters the acceptable range.
- The logic intentionally stays indicator-free: only raw quotes are required, so the strategy works on any timeframe or instrument that provides Level1 data.

## Classification

- **Category**: Utilities / Alerts
- **Trading Direction**: None
- **Execution Style**: Event-driven monitoring
- **Data Requirements**: Level1 bid/ask
- **Complexity**: Basic
- **Recommended Timeframe**: Any (quote-driven)
- **Risk Management**: Not applicable (no positions opened)

This documentation summarizes the StockSharp implementation and highlights the practical steps needed to reproduce the MetaTrader alerting workflow inside the platform.
