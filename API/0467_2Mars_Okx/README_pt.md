# Estratégia 2Mars OKX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um cruzamento de médias móveis com um filtro SuperTrend. As Bollinger Bands fornecem alvos de lucro enquanto um stop loss baseado em ATR limita o risco.

## Regras
- **Comprado**: A EMA de sinal cruza acima da EMA base e o preço está acima do SuperTrend.
- **Vendido**: A EMA de sinal cruza abaixo da EMA base e o preço está abaixo do SuperTrend.
- **Saída**: Realização de lucro na banda superior ou inferior de Bollinger, ou stop loss no ATR multiplicado por um fator.

## Indicadores
- EMA
- SuperTrend
- Bollinger Bands
- Average True Range
