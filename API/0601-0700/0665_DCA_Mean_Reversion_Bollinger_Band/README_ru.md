# Стратегия DCA с возвращением к среднему и полосами Боллинджера
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Покупает фиксированную сумму долларов при пересечении цены ниже нижней полосы Боллинджера или в первый день каждого месяца. Все позиции закрываются в заданную дату.

## Параметры
- `InvestmentAmount` - сумма покупки
- `OpenDate` - начало покупок
- `CloseDate` - дата закрытия всех позиций
- `StrategyMode` - BB mean reversion, monthly DCA или комбинированный режим
- `BollingerPeriod` - период полос Боллинджера
- `BollingerMultiplier` - множитель стандартного отклонения
- `CandleType` - таймфрейм для расчёта полос

## Индикаторы
- Полосы Боллинджера
