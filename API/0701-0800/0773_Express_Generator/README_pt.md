# Estratégia Express Generator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de médias móveis confirmados por sinais de RSI e MACD. O tamanho da posição usa um fator de volatilidade baseado em ATR e uma porcentagem de risco fixa. Um stop trailing em pips gerencia as saídas.

## Detalhes

- **Entrada Comprado**: SMA rápida cruza acima da SMA lenta, RSI abaixo de sobrecomprado, linha MACD cruza acima do sinal.
- **Entrada Vendido**: SMA rápida cruza abaixo da SMA lenta, RSI acima de sobrevendido, linha MACD cruza abaixo do sinal.
- **Saída**: Stop trailing em pips.
- **Tamanho da posição**: % de risco do patrimônio dividido pela distância do stop ajustada pelo ATR.
- **Indicadores**: SMA, RSI, MACD, ATR.
- **Direção**: Ambas as direções.
