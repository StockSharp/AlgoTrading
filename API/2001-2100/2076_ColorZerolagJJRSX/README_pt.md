# Estratégia Color Zerolag JJRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica do especialista **ColorZerolagJJRSX** do MetaTrader. Utiliza dois osciladores RSI suavizados para aproximar o indicador ColorZerolagJJRSX original. O cruzamento das linhas rápida e lenta gera sinais de negociação.

## Como funciona

- Quando o oscilador rápido cruza **abaixo** do oscilador lento, a estratégia fecha qualquer posição vendida e opcionalmente abre uma nova posição comprada.
- Quando o oscilador rápido cruza **acima** do oscilador lento, a estratégia fecha qualquer posição comprada e opcionalmente abre uma nova posição vendida.
- Níveis de stop-loss e take-profit de proteção são aplicados usando o mecanismo integrado `StartProtection`.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `FastPeriod` | Período da linha JJRSX rápida. |
| `SlowPeriod` | Período da linha JJRSX lenta. |
| `BuyOpen` | Permitir abertura de posições compradas. |
| `SellOpen` | Permitir abertura de posições vendidas. |
| `BuyClose` | Fechar posições compradas existentes no sinal oposto. |
| `SellClose` | Fechar posições vendidas existentes no sinal oposto. |
| `StopLoss` | Nível de stop-loss em unidades de preço. |
| `TakeProfit` | Nível de take-profit em unidades de preço. |
| `CandleType` | Período utilizado para os cálculos. |

## Notas

- A implementação usa indicadores integrados e a API `Bind` de alto nível.
- O volume é obtido da propriedade `Volume` da estratégia.
- Não há versão em Python para esta estratégia.

## Referências

O código-fonte MQL original está localizado em `MQL/13854` neste repositório.
