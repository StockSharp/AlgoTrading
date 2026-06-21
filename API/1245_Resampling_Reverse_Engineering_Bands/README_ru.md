# Resampling Reverse Engineering Bands
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Resampling Reverse Engineering Bands строит уровни цен RSI на основе ресэмплинга. Стратегия покупает, когда цена опускается ниже нижней полосы, и продаёт, когда цена поднимается выше верхней полосы.

## Детали
- **Данные**: Свечи цен.
- **Условия входа**:
  - **Лонг**: Цена закрытия ниже нижней полосы RRSI.
  - **Шорт**: Цена закрытия выше верхней полосы RRSI.
- **Условия выхода**: Противоположный сигнал.
- **Стопы**: Нет.
- **Параметры по умолчанию**:
  - `RsiPeriod` = 14
  - `HighThreshold` = 70
  - `LowThreshold` = 30
  - `SampleLength` = 1
- **Фильтры**:
  - Категория: Momentum
  - Направление: Long & Short
  - Индикаторы: RSI
  - Сложность: Средняя
  - Уровень риска: Средний
