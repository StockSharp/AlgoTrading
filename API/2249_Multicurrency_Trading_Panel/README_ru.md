# Стратегия Multicurrency Trading Panel
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия повторяет логику оригинального MQL советника «Multicurrency trading panel». Она отслеживает три валютные пары (EURUSD, USDJPY, GBPUSD) и сравнивает последнюю свечу с предыдущей по семи простым критериям (open, high, low, (high+low)/2, close, (high+low+close)/3, (high+low+close+close)/4).
За каждое сравнение увеличивается счётчик BUY или SELL. При включённом автоторговле стратегия открывает или разворачивает позицию по паре, если счётчик BUY превышает SELL или наоборот.

## Параметры
- **EURUSD** – первая бумага.
- **USDJPY** – вторая бумага.
- **GBPUSD** – третья бумага.
- **Candle Type** – таймфрейм свечей.
- **Auto Trade** – переключатель автоматической торговли.

Стратегия предназначена для демонстрационных целей и не подходит для реальной торговли.
