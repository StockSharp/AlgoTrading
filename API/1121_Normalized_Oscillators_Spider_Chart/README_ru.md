# Стратегия Normalized Oscillators Spider Chart
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия рассчитывает несколько осцилляторов (RSI, Stochastic, Correlation, Money Flow Index, Williams %R, Percent Up, Chande Momentum Oscillator и Aroon Oscillator). Все значения нормализуются в диапазон 0-1 и усредняются для формирования сигналов. Покупка совершается при среднем выше 0.6, продажа при падении ниже 0.4.

## Параметры
- **Length** — длина расчёта осцилляторов
- **Candle type** — таймфрейм используемых свечей

## Примечания
Упрощённый порт скрипта TradingView "Normalized Oscillators Spider Chart [LuxAlgo]", демонстрирующий использование индикаторов в StockSharp.
