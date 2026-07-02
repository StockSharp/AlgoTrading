# Стратегия Supply Demand Engulfment
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия торгует паттерны бычьего и медвежьего поглощения около зон поддержки и сопротивления Donchian.

## Детали

- **Вход**: Поглощение на границе зоны.
- **Лонг/Шорт**: Оба направления.
- **Выход**: Противоположный сигнал.
- **Стопы**: Нет.
- **Значения по умолчанию**:
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
