# Equivolume Overlay Volume Bars Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy renders volume-weighted boxes to emulate equivolume bars over price candles. It calculates a running sum of volume and scales each box width relative to recent activity. A moving average of volume can be configured.

The strategy does not place trades; it is intended as a visual example of using the StockSharp high-level API for custom chart drawing.
