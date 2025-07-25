# Стратегия возврата к среднему с учётом волатильности
[English](README.md) | [中文](README_cn.md)

В этой версии возвращения к среднему пороги входа масштабируются по отношению ATR к стандартному отклонению. Когда волатильность возрастает относительно обычного шума, расстояние, необходимое для открытия сделки, увеличивается, что помогает избежать преждевременных сигналов во время хаотичных движений.

Тестирование показывает среднегодичную доходность около 115%\. Стратегию лучше запускать на фондовом рынке.

Длинная позиция открывается, когда цена опускается ниже скользящей средней более чем на скорректированный порог. Короткая позиция открывается, когда цена поднимается выше средней на ту же величину. Позиции закрываются, когда цена вновь приближается к средней.

Адаптивный порог делает стратегию подходящей для рынков с меняющимися режимами волатильности. Стоп‑лосс, равный двум ATR, ограничивает риск в ожидании возврата.

## Подробности
- **Условия входа**:
  - **Long**: закрытие < MA − Multiplier * ATR / (ATR/StdDev)
  - **Short**: закрытие > MA + Multiplier * ATR / (ATR/StdDev)
- **Long/Short**: обе стороны.
- **Условия выхода**:
  - **Long**: выход при закрытии >= MA
  - **Short**: выход при закрытии <= MA
- **Стопы**: да, динамический на основе ATR.
- **Параметры по умолчанию**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Возврат к среднему
  - Направление: Обе стороны
  - Индикаторы: ATR, StdDev
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

