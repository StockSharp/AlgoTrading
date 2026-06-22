# Estratégia Color Bulls
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma portagem do especialista MetaTrader `Exp_ColorBulls`. Ela se baseia no indicador Color Bulls, que calcula a diferença entre o preço máximo da vela e uma média móvel. O valor resultante é suavizado por outra média móvel e exibido como um histograma com cores diferentes para valores em alta e em baixa.

A estratégia reage às mudanças de cor deste histograma:

- Quando o indicador muda de subindo (verde) para caindo (magenta), uma posição comprada é aberta.
- Quando o indicador muda de caindo para subindo, uma posição vendida é aberta.
- Posições opostas são fechadas automaticamente antes de abrir novas.

Apenas velas concluídas são processadas e ordens a mercado são usadas para entradas e saídas.

## Parâmetros

- **Fast MA Length** – período da média móvel aplicada aos preços máximos.
- **Smooth Length** – período da média móvel utilizada para suavizar o valor bulls.
- **Candle Type** – período das velas utilizadas para os cálculos.

## Notas

Este exemplo demonstra a integração de um indicador personalizado com a API de alto nível do StockSharp. O gerenciamento de stop-loss e take-profit não está incluído.
