# Linear Mean Reversion Strategy
[English](README.md) | [中文](README_cn.md)

**Linear Mean Reversion Strategy** — стратегия возврата к среднему, основанная на z-скор (число стандартных отклонений цены от среднего). Использует фиксированный стоп-лосс в пунктах.

## Подробности
- **Данные**: ценовые свечи.
- **Условия входа**:
  - **Лонг**: z-score < -EntryThreshold.
  - **Шорт**: z-score > EntryThreshold.
- **Условия выхода**: z-score возвращается к нулю (для лонга z-score > -ExitThreshold, для шорта z-score < ExitThreshold).
- **Стопы**: фиксированный стоп-лосс в пунктах.
- **Параметры по умолчанию**:
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **Фильтры**:
  - Категория: возврат к среднему
  - Направление: лонг и шорт
  - Индикаторы: SMA, StandardDeviation
  - Стопы: да
  - Сложность: низкая
  - Уровень риска: средний
