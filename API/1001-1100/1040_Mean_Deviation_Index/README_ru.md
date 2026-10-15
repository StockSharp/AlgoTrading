# Стратегия Mean Deviation Index
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия торгует отклонения цены от ATR-фильтрованной EMA.
Покупка открывается при значении MDX выше уровня,
продажа – при значении ниже отрицательного уровня.

## Детали

- **Вход**:
  - Лонг при MDX > Level
  - Шорт при MDX < -Level
- **Выход**: противоположный сигнал.
- **Индикаторы**: EMA и ATR.
