# Estratégia de Gráfico Aranha com Osciladores Normalizados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula múltiplos osciladores (RSI, Stochastic, Correlation, Money Flow Index, Williams %R, Percent Up, Chande Momentum Oscillator e Aroon Oscillator). Todos os valores são normalizados no intervalo 0-1 e calculados em média para gerar sinais de negociação. A estratégia compra quando a média supera 0,6 e vende a descoberto quando cai abaixo de 0,4.

## Entradas
- **Length** — período de lookback para todos os osciladores
- **Candle type** — período das velas utilizadas

## Notas
Esta é uma versão simplificada do script do TradingView "Normalized Oscillators Spider Chart [LuxAlgo]" demonstrando o uso de indicadores no StockSharp.
