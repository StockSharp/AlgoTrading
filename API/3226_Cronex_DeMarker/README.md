# Cronex DeMarker
[Русский](README_ru.md) | [中文](README_cn.md)

The **Cronex DeMarker Strategy** reproduces the classic Cronex expert advisor that combines the DeMarker oscillator with a double smoothing stack. First, the DeMarker values are smoothed by a fast simple moving average, then the result is smoothed once more by a slower average. The distance and relative order of these two lines provide reversal-style entry signals.

The original MQL5 implementation allows trade direction toggles and works on higher timeframes. This StockSharp port keeps the same philosophy: it reacts when the fast line crosses through the slow one and immediately closes any opposite position. Because the system is contrarian, a cross below the slow line opens a long position, while a cross above opens a short. Both directions can be disabled independently through parameters, making the strategy flexible for different portfolio allocations.

## How it works

1. Request candles for the selected timeframe (4H by default).
2. Calculate the DeMarker oscillator and smooth it with a fast SMA (default 14 bars).
3. Apply a second SMA (default 25 bars) on top of the fast line to obtain the signal line.
4. When the fast line was above the slow line on the previous candle and now drops below it, the strategy buys (contrarian reversal). Any existing short position is flattened.
5. When the fast line was below the slow line on the previous candle and now climbs above it, the strategy sells and closes any open long.
6. Position size is defined by the `Volume` property; reversals use the absolute position to flip immediately.

This logic allows the expert to capture short-term exhaustion moves after strong momentum pushes, making it a mean-reversion tool that prefers ranging or choppy markets.

## Default parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `DeMarkerPeriod` | 25 | Number of bars used by the DeMarker oscillator. |
| `FastPeriod` | 14 | Length of the first smoothing SMA applied to DeMarker values. |
| `SlowPeriod` | 25 | Length of the signal SMA applied to the fast line. |
| `CandleType` | 4 hour | Candle series used for indicator calculations. |
| `EnableLongEntry` | true | Allow contrarian long entries when the fast line crosses below the slow line. |
| `EnableShortEntry` | true | Allow short entries when the fast line crosses above the slow line. |
| `EnableLongExit` | true | Close existing long positions when bearish conditions appear. |
| `EnableShortExit` | true | Close existing short positions when bullish conditions appear. |

## Filters & tags

- **Category**: Mean Reversion, Oscillator based
- **Direction**: Long & Short (configurable)
- **Indicators**: DeMarker, Simple Moving Average (double smoothing)
- **Stops**: None (fully signal driven)
- **Timeframe**: Swing trading (H4 by default, adjustable)
- **Complexity**: Intermediate due to sequential indicator chain
- **Risk Profile**: Medium — contrarian entries can face extended trends
- **Automation**: Fully automated via high-level StockSharp API

## Usage notes

- The strategy only processes finished candles to avoid repainting issues.
- Reversal orders reuse the absolute position size, guaranteeing immediate flattening before entering the new direction.
- Chart output draws the two smoothed lines and trade markers, helping with discretionary validation.
- For portfolios that only allow one direction, disable the unwanted entries and exits through the provided parameters.
- Consider adding external risk controls (stop-loss, trailing exit) when deploying on volatile assets.
