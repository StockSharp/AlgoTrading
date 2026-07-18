# Estratégia CCI Woodies
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia opera com base no cruzamento de duas linhas do Índice de Canal de Commodities (CCI) derivadas do método CCI de Woodies. Um CCI rápido e um CCI lento são calculados no período especificado. Quando a linha rápida cruza abaixo da linha lenta, uma posição comprada é aberta e qualquer posição vendida é fechada. Quando a linha rápida cruza acima da linha lenta, uma posição vendida é aberta e qualquer posição comprada é fechada.

## Parâmetros
- **FastPeriod** – comprimento do indicador CCI rápido.
- **SlowPeriod** – comprimento do indicador CCI lento.
- **CandleType** – período das velas usadas para os cálculos.
- **InvertSignals** – se habilitado, as regras de compra e venda são trocadas.
- **TakeProfitPoints** – meta de lucro em pontos de preço.
- **StopLossPoints** – limite de perda em pontos de preço.

## Notas
A estratégia usa a API de alto nível do StockSharp. Os indicadores são vinculados via `Bind`, e o controle de risco é gerenciado com `StartProtection` usando níveis de stop-loss e take-profit.
