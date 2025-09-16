# Boll Trade Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the original **BollTrade** expert advisor by trading Bollinger Band breakouts with a configurable
pip buffer and optional balance-based position sizing. Orders are opened only on completed candles and are managed with
static stop-loss and take-profit levels.

## Concept

- Subscribes to the configurable primary timeframe and calculates a Bollinger Bands envelope with the specified period and
  deviation.
- Adds an extra offset (`Band Offset`) measured in pip units on top of the upper band and below the lower band to reduce
  premature entries.
- Opens a **long** position when the candle close finishes below the lower band minus the offset.
- Opens a **short** position when the candle close finishes above the upper band plus the offset.
- Only one position can be active at any time. The strategy waits for the current trade to finish before evaluating new
  entries.

## Trade Management

- Stop-loss and take-profit levels are set immediately after an entry. They are expressed in pip multiples and evaluated on
  every completed candle. If price touches either level the position is closed at market.
- If `Scale Volume` is enabled the traded volume grows (or shrinks) with the account balance. The scaling baseline is the
  starting portfolio value divided by the base lot size, mimicking the original MQL implementation. Volume is capped at 500
  lots to keep risk under control just like in the source code.
- The pip size is derived from the security price step. For very small steps (forex-style symbols) the code multiplies the
  step by 10 to convert fractional pip steps into standard pips, matching the behaviour of the MetaTrader version.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Candle Type` | Timeframe used for signal candles. | 15-minute time frame |
| `Bollinger Period` | Number of bars in the Bollinger Bands calculation. | 4 |
| `Bollinger Deviation` | Width multiplier for the Bollinger Bands. | 2 |
| `Band Offset` | Additional pip offset added outside both bands before triggering signals. | 3 |
| `Take Profit (pips)` | Distance to the profit target in pip units. | 3 |
| `Stop Loss (pips)` | Distance to the protective stop in pip units. | 20 |
| `Base Volume` | Default volume in lots used when scaling is disabled. | 1 |
| `Scale Volume` | When enabled, scales position size with the account balance. | Enabled |

## Usage Notes

- Works best on forex or CFD symbols where pip-based offsets provide clear breakout levels, but it can also run on futures or
  equities provided their `PriceStep` is configured.
- The strategy processes only finished candles, so intrabar spikes that revert before the bar closes will not trigger
  entries.
- Because exits are handled with fixed stops and targets, ensure those distances are appropriate for the selected timeframe
  and instrument volatility.
- The original EA relied on broker-side stops. This port monitors candle extremes to emulate the same protective behaviour
  inside StockSharp.

## Files

- `CS/BollTradeStrategy.cs` – C# implementation of the strategy.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.

No Python translation is provided yet, as requested.
