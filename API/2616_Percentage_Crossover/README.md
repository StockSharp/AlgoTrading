# Percentage Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the behaviour of the original MetaTrader expert `Exp_PercentageCrossover`. It trades the direction of the Percentage Crossover indicator, which draws a trailing price line that can only move within a fixed percentage band around the current close. The slope of this line defines the state of the market and triggers trades.

## Concept

1. On every completed candle the indicator keeps the previous line value.
2. A bullish update is made when the close pushes the trailing line above its prior value by at least `percent` percent of price.
3. A bearish update is made when the close drags the trailing line below its prior value by the same percent.
4. If the close remains within the band, the line stays flat and retains its last colour.

The colour of the line is interpreted in the same way as in MetaTrader:

- **Colour index 0 (blue/violet)** – the line is rising (bullish context).
- **Colour index 1 (orange)** – the line is falling (bearish context).

## Trading rules

### Long entries
- Enabled only when `BuyPosOpen = true`.
- Evaluate the bar selected by `SignalBar` (1 means the last closed bar).
- Open a long position when that bar switches from colour 1 to colour 0.

### Short entries
- Enabled only when `SellPosOpen = true`.
- Evaluate the same `SignalBar` bar.
- Open a short position when the bar switches from colour 0 to colour 1.

### Position management
- If `BuyPosClose = true`, any open long position is closed whenever the current bar (after applying the `SignalBar` offset) is colour 1.
- If `SellPosClose = true`, any open short position is closed whenever that bar is colour 0.
- When `UseTimeFilter = true` and the current time is outside the configured trading window the strategy immediately exits the active position and ignores new signals until the market re-enters the window.
- Orders are sent with `BuyMarket()` and `SellMarket()`. The actual quantity comes from the strategy `Volume` property.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Percent` | Percentage band for the trailing line. Higher values make the line react slower. | `1` |
| `SignalBar` | Which closed bar is analysed (1 = last closed). Must remain positive. | `1` |
| `BuyPosOpen` / `SellPosOpen` | Enable long or short entries respectively. | `true` |
| `BuyPosClose` / `SellPosClose` | Enable closing logic for long or short positions. | `true` |
| `UseTimeFilter` | Activate the trading window. | `true` |
| `StartHour` / `StartMinute` | Hour and minute that open the trading window when the filter is active. | `0` / `0` |
| `EndHour` / `EndMinute` | Hour and minute that close the trading window. | `23` / `59` |
| `CandleType` | Time frame of the candles used for the indicator and signals. | `4h` |

## Notes

- The time filter follows the original Expert Advisor strictly. When the start hour is greater than the end hour the logic creates an overnight window, but it still requires the minutes to be greater than or equal to `StartMinute` before the session becomes active.
- `SignalBar` is evaluated on finished candles only. Set it to `1` to mirror the default MetaTrader configuration.
- No stop-loss or take-profit levels are imposed by the strategy. Risk control must be handled externally or by tuning the percentage and the trading window.
