# Стратегия Rsi Williams R
[English](README.md) | [中文](README_cn.md)

Реализация стратегии №163 — RSI + Williams %R. Покупка, когда RSI ниже 30 и Williams %R ниже -80 (двойная перепроданность). Продажа, когда RSI выше 70 и Williams %R выше -20 (двойная перекупленность).

Тестирование показывает среднегодичную доходность около 76%\. Стратегию лучше запускать на рынке Форекс.

RSI показывает общий импульс, а Williams %R дает более быстрый сигнал разворота. Сделки открываются при согласовании двух осцилляторов.

Подходит активным трейдерам, работающим на коротких движениях. Стоп-лосс основан на ATR.

## Подробности

- **Условия входа**:
  - Длинная: `RSI < RsiOversold && WilliamsR < WilliamsROversold`
  - Короткая: `RSI > RsiOverbought && WilliamsR > WilliamsROverbought`
- **Long/Short**: Оба
- **Условия выхода**:
  - RSI возвращается в нейтральную зону
- **Стопы**: процентный уровень через `StopLoss`
- **Параметры по умолчанию**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Оба
  - Индикаторы: RSI, Williams %R, R
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

