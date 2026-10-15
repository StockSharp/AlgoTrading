# Estratégia de Cruzamento TSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa um sistema de cruzamento baseado no True Strength Index (TSI) e sua linha de sinal de média móvel exponencial.

A estratégia assina velas de 4 horas por padrão e calcula o TSI usando comprimentos de suavização curto e longo configuráveis. Um EMA adicional produz a linha de sinal. Uma posição comprada é aberta quando o TSI cruza acima da linha de sinal; uma posição vendida é aberta quando o TSI cruza abaixo da linha de sinal. Posições opostas são fechadas automaticamente no cruzamento inverso.

- Indicadores: True Strength Index, Exponential Moving Average
- Parâmetros:
  - `CandleType` – série de velas a processar.
  - `LongLength` – período de suavização longo para TSI.
  - `ShortLength` – período de suavização curto para TSI.
  - `SignalLength` – período da linha de sinal EMA.
