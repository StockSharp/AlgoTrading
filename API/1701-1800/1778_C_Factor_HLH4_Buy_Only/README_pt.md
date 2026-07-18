# Estratégia C-Factor HLH4 Somente Compra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução em C# do consultor especialista MQL original **C_Factor_HLH4_buy_only**. Demonstra como portar estratégias do MetaTrader para a API de alto nível do StockSharp.

## Lógica da estratégia

- Utiliza velas de período de quatro horas.
- Abre uma posição comprada quando a vela atual fecha acima da máxima da vela anterior.
- Sai da posição comprada quando o preço de fechamento:
  - supera a mínima da vela anterior em 100 ticks, ou
  - cai abaixo da máxima da vela anterior em 20 ticks.
- A gestão de risco é tratada com distâncias de stop-loss e take-profit configuráveis.
- O volume da ordem é calculado a partir do percentual do patrimônio da conta arriscado por operação.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `StopLoss` | Distância em ticks para o stop de proteção. |
| `TakeProfit` | Distância em ticks para a meta de lucro. |
| `RiskPercent` | Percentual do patrimônio da conta arriscado em cada operação. |
| `CandleType` | Tipo de vela e período para análise (padrão: velas de 4 horas). |

## Observações

A estratégia opera apenas comprado e é projetada para fins educativos. Ajuste os parâmetros e as configurações de risco antes de usá-la no trading real.
