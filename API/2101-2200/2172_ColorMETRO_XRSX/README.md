# ColorMETRO XRSX Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy is a StockSharp implementation inspired by the original MQL5 Expert Advisor "Exp_ColorMETRO_XRSX". It uses two smoothed moving averages to detect trend changes. A long position is opened when the fast average crosses above the slow average, while a short position is opened when the fast average crosses below the slow average.

## Parameters

- **Fast Period** – period of the fast moving average.
- **Slow Period** – period of the slow moving average.
- **Candle Type** – time frame of candles used for calculations.

## How It Works

1. The strategy subscribes to the selected candle series.
2. Two `Sma` indicators with different periods are calculated on the close price.
3. When the fast SMA crosses above the slow SMA, any short position is closed and a long position is opened.
4. When the fast SMA crosses below the slow SMA, any long position is closed and a short position is opened.
5. The previous values of the averages are stored to detect crossings only once.

## Notes

- The strategy uses the high level API with `Bind` for indicator processing.
- `StartProtection` is enabled to manage protective mechanisms.
- This implementation is a simplified translation and does not use the original custom indicator.
