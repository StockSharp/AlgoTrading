# Стратегия Divergence For Many Indicators
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Определяет бычьи и медвежьи дивергенции между ценой и RSI и гистограммой MACD. Когда количество сигналов достигает заданного порога, стратегия открывает позицию в противоположную сторону.

## Параметры
- `RsiPeriod` – период RSI.
- `MacdFastPeriod` – быстрый период MACD.
- `MacdSlowPeriod` – медленный период MACD.
- `MacdSignalPeriod` – период сигнальной линии MACD.
- `MinDivergence` – минимальное число индикаторов, подтверждающих дивергенцию.
- `CandleType` – тип свечей.
