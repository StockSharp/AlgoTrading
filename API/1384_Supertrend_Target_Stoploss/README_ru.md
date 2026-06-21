# Supertrend Target Stop
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия покупает при пересечении цены выше линии Supertrend и продаёт при пересечении ниже. Фиксированные проценты тейк-профита и стоп-лосса закрывают позиции.

## Детали

- **Вход**: Пересечение цены и линии Supertrend.
- **Лонг/Шорт**: Оба направления.
- **Выход**: По достижении цели или стоп-лосса.
- **Стопы**: Да, фиксированный процент.
- **Значения по умолчанию**:
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
