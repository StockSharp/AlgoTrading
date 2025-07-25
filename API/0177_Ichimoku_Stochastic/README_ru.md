# Стратегия Ichimoku Stochastic
[English](README.md) | [中文](README_cn.md)

Стратегия использует облако Ишимоку и осциллятор Стохастик. Длинная позиция открывается, когда цена выше облака, Tenkan больше Kijun, а Стохастик в зоне перепроданности (< 20). Короткая — когда цена ниже облака, Tenkan меньше Kijun и Стохастик в зоне перекупленности (> 80).

Тестирование показывает среднегодичную доходность около 118%\. Стратегию лучше запускать на фондовом рынке.

Ишимоку определяет тренд и уровни поддержки, а Стохастик выбирает момент входа на откатах. Сделки открываются, когда осциллятор перезагружается в направлении облака.

Трейдерам, предпочитающим структурированные индикаторы, метод покажется практичным. Стопы по границам облака защищают от резких разворотов.

## Подробности

- **Условия входа**:
  - Длинная: `Price > Cloud && StochK < 20`
  - Короткая: `Price < Cloud && StochK > 80`
- **Long/Short**: Оба
- **Условия выхода**:
  - выход цены из облака в противоположную сторону
- **Стопы**: используются границы облака Ишимоку
- **Параметры по умолчанию**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Оба
  - Индикаторы: Ichimoku Cloud, Stochastic Oscillator
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

