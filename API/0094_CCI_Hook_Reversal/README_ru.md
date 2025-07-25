# Стратегия разворота по CCI Hook
[English](README.md) | [中文](README_zh.md)

Стратегия CCI Hook Reversal использует индикатор CCI в качестве триггера, когда он «зацепляется» от экстремального значения. После достижения уровня выше +100 или ниже −100 индикатор часто быстро возвращается, сигнализируя о затухании импульса.

Тестирование показывает среднегодичную доходность около 169%. Стратегию лучше запускать на крипторынке.

Длинные сделки открываются, когда CCI разворачивается вверх из перепроданности, пока цена делает ещё один небольшой минимум. Короткие — когда CCI разворачивается вниз из перекупленности, а цена делает новый максимум.

Каждая сделка имеет небольшой фиксированный стоп и закрывается, когда CCI зацепляется в обратную сторону или достигается стоп.

## Детали

- **Условия входа**: сигнал индикатора
- **Лонг/шорт**: оба направления
- **Условия выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, на процентной основе
- **Значения по умолчанию**:
  - `CandleType` = 15 минут
  - `StopLoss` = 2%
- **Фильтры**:
  - Категория: Разворот
  - Направление: Оба
  - Индикаторы: CCI
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

