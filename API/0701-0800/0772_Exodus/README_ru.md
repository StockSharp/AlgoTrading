# Стратегия Exodus
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Упрощённая адаптация скрипта TradingView **EXODUS** для StockSharp. Стратегия использует объёмно-взвешенный импульс VWMO и индикатор ADX для поиска сильных направленных движений.

## Детали

- **Условия входа**
  - Лонг: `VWMO > VwmoThreshold` и `ADX > AdxThreshold`.
  - Шорт: `VWMO < -VwmoThreshold` и `ADX > AdxThreshold`.
- **Условия выхода**
  - Импульс пересекает ноль или появляется обратный сигнал.
- **Индикаторы**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Параметры**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`

**Точная упрощённая формула VWMO и параметры по умолчанию:** `VwmoMomentum=10`, `VwmoVolume=20`, `VwmoSmooth=3`, `VwmoThreshold=25`, `AdxLength=14`, `AdxThreshold=25`, `AtrLength=14`, `AtrMultiplier=2`, `TpMultiplier=3`, `CandleType=1 минута`. Для каждой закрытой свечи `i` вычисляется `d_i=Close_i−Close_(i−VwmoMomentum)`; сырой `VWMO=100 × Σ(d_i×Volume_i) / Σ(|d_i|×Volume_i)` за последние `VwmoVolume` свечей (ноль при нулевом знаменателе), затем SMA от последних `VwmoSmooth` значений. Это нормализованный **знаковый объёмно-взвешенный импульс** (−100..100), а не пересечение EMA. После формирования VWMO и настоящего ADX покупка требует `VWMO > +VwmoThreshold && ADX > AdxThreshold`; продажа — `VWMO < −VwmoThreshold && ADX > AdxThreshold`. Противоположный сигнал разворачивает всю позицию одной рыночной заявкой. Без такого сигнала лонг закрывается при переходе VWMO с положительного значения на неположительное, шорт — с отрицательного на неотрицательное. Стоп задаётся нативной защитой на расстоянии `Wilder ATR(AtrLength) × AtrMultiplier`, тейк — `расстояние стопа × TpMultiplier`. Защита отслеживает Level1 bid/ask между свечами. При активной заявке повторные заявки запрещены, будущие свечи не используются.
