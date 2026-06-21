# Estrategia de Gráfico de Araña con Osciladores Normalizados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula múltiples osciladores (RSI, Stochastic, Correlation, Money Flow Index, Williams %R, Percent Up, Chande Momentum Oscillator y Aroon Oscillator). Todos los valores se normalizan en el rango 0-1 y se promedian para generar señales de trading. La estrategia compra cuando el promedio supera 0.6 y vende en corto cuando cae por debajo de 0.4.

## Entradas
- **Length** — período de lookback para todos los osciladores
- **Candle type** — marco temporal de las velas utilizadas

## Notas
Este es un port simplificado del script de TradingView "Normalized Oscillators Spider Chart [LuxAlgo]" que demuestra el uso de indicadores en StockSharp.
