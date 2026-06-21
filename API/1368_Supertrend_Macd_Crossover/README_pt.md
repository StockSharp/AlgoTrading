# Estratégia de Cruzamento MACD com Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o indicador Supertrend com um cruzamento de MACD para identificar entradas de alta.
Uma posição comprada é aberta quando o preço está acima da linha Supertrend e a linha MACD cruza acima de sua linha de sinal.
A posição é fechada quando o preço cai abaixo da linha Supertrend e a linha MACD cruza abaixo de seu sinal.

## Detalhes

- **Indicadores**: Supertrend (ATR 10, fator 3), MACD (12, 26, 9)
- **Entrada**: Preço acima do Supertrend e cruzamento altista do MACD
- **Saída**: Preço abaixo do Supertrend e cruzamento baixista do MACD
- **Direção**: Somente comprado
- **Período**: Qualquer
