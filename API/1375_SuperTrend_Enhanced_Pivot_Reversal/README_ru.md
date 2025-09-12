# Стратегия SuperTrend Enhanced Pivot Reversal
[English](README.md) | [中文](README_cn.md)

Комбинирует направление SuperTrend с пробоем pivot‑уровней. Стоп‑заявка на покупку ставится выше последнего pivot high при нисходящем SuperTrend. Стоп‑заявка на продажу — ниже pivot low при восходящем SuperTrend. Позиции защищаются процентным стоп‑лоссом от pivot.

## Детали

- **Вход**:
  - Лонг: сформирован pivot high, SuperTrend вниз → стоп‑покупка выше pivot.
  - Шорт: сформирован pivot low, SuperTrend вверх → стоп‑продажа ниже pivot.
- **Направление**: настраиваемое.
- **Выход**: процентный стоп‑лосс или смена направления для одностороннего режима.
- **Индикаторы**: SuperTrend, pivot high/low.
- **Значения по умолчанию**:
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 минут
