# Estratégia de Velocidade de Volume ValueWhen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca entradas compradas quando o volume se expande, o mercado está sobrevendido com base no RSI, a volatilidade medida pelo ATR está se contraindo e a distância entre os rompimentos recentes da SMA excede um valor especificado. Quando todas as condições são satisfeitas, uma ordem de compra a mercado é emitida.

## Parâmetros
- **RSI Length** – período para o RSI.
- **RSI Oversold** – limiar de sobrevenda.
- **ATR Small / ATR Big** – períodos para comparação de ATR.
- **Distance** – diferença mínima entre preços de rompimento.
- **Candle Type** – período das velas de entrada.
