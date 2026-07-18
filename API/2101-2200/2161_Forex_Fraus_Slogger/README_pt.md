# Estratégia Forex Fraus Slogger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o sistema de reversão por envelope do MetaTrader.

## Lógica

- Calcula uma SMA de 1 período como preço base.
- Os envelopes superior e inferior são definidos a `EnvelopePercent` por cento a partir da base.
- Quando o preço fecha acima da banda superior e depois retorna abaixo, entra-se em posição vendida.
- Quando o preço fecha abaixo da banda inferior e depois retorna acima, entra-se em posição comprada.
- As posições são protegidas por um stop trailing.

## Parâmetros

- `EnvelopePercent` – deslocamento percentual para os envelopes (padrão 0.1).
- `TrailingStop` – distância do stop trailing em unidades de preço (padrão 0.001).
- `TrailingStep` – movimento mínimo de preço necessário para avançar o stop trailing (padrão 0.0001).
- `ProfitTrailing` – habilitar trailing somente após a posição tornar-se lucrativa.
- `UseTimeFilter` – operar apenas durante as horas especificadas.
- `StartHour` – início da janela de operações.
- `StopHour` – fim da janela de operações.
- `CandleType` – período de velas utilizado para os cálculos.

## Notas

- A estratégia utiliza ordens a mercado via `BuyMarket` e `SellMarket`.
- O stop trailing encerra a posição quando o preço cruza o nível de stop.
