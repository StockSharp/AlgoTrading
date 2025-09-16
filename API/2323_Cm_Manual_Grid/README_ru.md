# Cm Manual Grid
[English](README.md) | [中文](README_cn.md)

Cm Manual Grid размещает настраиваемую сетку стоп- и лимитных заявок вокруг текущей цены. Каждый новый ордер увеличивает объем на фиксированную величину. Стратегия может закрывать длинные или короткие позиции по отдельности при достижении целевой прибыли и содержит механизм трейлинга прибыли.

## Детали

- **Тип**: сеточная торговля отложенными ордерами
- **Ордеры**: Buy Stop, Sell Stop, Buy Limit, Sell Limit
- **Объем**: начальный `Lot` с шагом `LotPlus`
- **Управление прибылью**:
  - `CloseProfitB` закрывает длинные позиции
  - `CloseProfitS` закрывает короткие позиции
  - `ProfitClose` закрывает все позиции
  - `TralStart` и `TralClose` управляют трейлингом
- **Значения по умолчанию**:
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 шагов
  - `StepBuyStop` = 10
  - `StepSellStop` = 10
  - `StepBuyLimit` = 10
  - `StepSellLimit` = 10
  - `Lot` = 0.1
  - `LotPlus` = 0.1
  - `CloseProfitB` = 10
  - `CloseProfitS` = 10
  - `ProfitClose` = 10
  - `TralStart` = 10
  - `TralClose` = 5
