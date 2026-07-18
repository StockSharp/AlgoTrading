# Lacust Stop and BE
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy demonstrates basic position management inspired by the original MQL expert advisor **lacuststopandbe**.

After entering a position in the direction of the last finished candle the strategy applies several protective rules:

- Initial stop loss and take profit are placed at fixed price distances.
- When profit reaches `BreakevenGain`, the stop is moved to entry price plus `Breakeven`.
- After profit exceeds `TrailingStart`, the stop trails behind price by `TrailingStop`.
- The position is closed when the stop level or take profit level is touched.

Parameters:

- `CandleType` – candle series used for processing.
- `StopLoss` – initial stop loss distance.
- `TakeProfit` – initial take profit distance.
- `TrailingStart` – profit required to activate trailing stop.
- `TrailingStop` – trailing stop distance from current price.
- `BreakevenGain` – profit required before moving stop to break-even.
- `Breakeven` – profit locked after moving stop to break-even.

This sample uses the high level StockSharp API and can serve as a template for porting simple MQL trade management scripts.
