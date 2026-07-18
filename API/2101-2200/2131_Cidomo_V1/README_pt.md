# Estratégia Cidomo V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento diário que coloca negociações quando o preço escapa do intervalo recente.

## Resumo

- **Tipo**: Rompimento
- **Entrada**: Compra quando o preço rompe acima da máxima mais alta do período de retrospectiva, vende quando o preço rompe abaixo da mínima mais baixa.
- **Saída**: Stop loss, take profit, breakeven e trailing stop opcionais.
- **Indicadores**: Highest, Lowest

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `Lookback` | Número de velas utilizadas para calcular o intervalo. |
| `Delta` | Deslocamento de preço adicionado aos níveis de rompimento. |
| `StopLoss` | Stop loss em pontos de preço. |
| `TakeProfit` | Take profit em pontos de preço. |
| `NoLoss` | Mover o stop para o nível de entrada após este lucro (pontos). |
| `Trailing` | Distância de trailing em pontos. |
| `UseTimeFilter` | Se verdadeiro, os níveis são calculados após o horário especificado. |
| `TradeTime` | Hora do dia para calcular os níveis de rompimento. |
| `CandleType` | Tipo de vela utilizado para os cálculos. |

## Notas

A estratégia monitora apenas velas concluídas. Os níveis são recalculados uma vez por dia após `TradeTime`.
