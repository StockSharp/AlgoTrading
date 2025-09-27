# Стратегия KumoTrade Ichimoku
[English](README.md) | [中文](README_cn.md)

Стратегия основана на индикаторах Ichimoku Cloud и Stochastic Oscillator.
Длинная позиция открывается при откате выше линии Kijun, при отсутствии облака впереди и перепроданном Stochastic.
Короткая позиция открывается при падении ниже облака, перекупленном Stochastic и медвежьем Kumo.

## Детали

- **Условия входа**:
  - Long: `Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - Short: `Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **Тип**: Лонг и шорт
- **Условия выхода**:
  - Скользящий стоп по ATR
- **Стопы**: Трейлинг стоп с ATR * 3
- **Значения по умолчанию**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Следование тренду
  - Направление: Оба
  - Индикаторы: Ichimoku Cloud, Stochastic, ATR
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
