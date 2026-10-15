# Стратегия Volume ValueWhen Velocity
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия открывает длинную позицию, когда объём растёт, рынок находится в состоянии перепроданности по RSI, волатильность по ATR снижается, а расстояние между двумя последними пробоями SMA превышает заданное значение.

## Параметры
- **RSI Length** – период RSI.
- **RSI Oversold** – уровень перепроданности.
- **ATR Small / ATR Big** – периоды ATR для сравнения.
- **Distance** – минимальная разница между ценами пробоя.
- **Candle Type** – таймфрейм входных свечей.
