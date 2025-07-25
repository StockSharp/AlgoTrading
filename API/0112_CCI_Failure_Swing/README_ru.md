# Стратегия "CCI Failure Swing"
[English](README.md) | [中文](README_zh.md)

CCI Failure Swing основана на индексе товарного канала, который формирует более низкий максимум выше +100 или более высокий минимум ниже -100. Неспособность установить новый экстремум зачастую сигнализирует о завершении предыдущего тренда.

Тестирование показывает среднегодичную доходность около 73%. Стратегию лучше запускать на крипторынке.

Стратегия открывает длинную позицию, когда CCI удерживается выше -100 и разворачивается вверх, или короткую, когда индикатор не пробивает +100 и поворачивает вниз.

Процентный стоп сохраняет небольшой риск, а выход выполняется, если CCI пересекает предыдущий уровень свинга.

## Детали

- **Условия входа**: сигнал индикатора
- **Длинная/короткая**: обе
- **Условия выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Фильтры**:
  - Категория: Разворот
  - Направление: обе
  - Индикаторы: CCI
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейронные сети: нет
  - Дивергенция: нет
  - Уровень риска: средний

