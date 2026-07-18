# RSI Especialista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia **RSI Especialista** opera usando o Índice de Força Relativa (RSI). Aguarda que o valor do RSI cruze níveis predefinidos de sobrecompra ou sobrevenda e entra em posições na direção do cruzamento.

## Lógica

- Calcular o RSI para cada vela.
- Quando o RSI cruza **acima** do nível de sobrevenda, uma posição comprada é aberta.
- Quando o RSI cruza **abaixo** do nível de sobrecompra, uma posição vendida é aberta.
- Antes de entrar em uma nova posição, a oposta é fechada.
- Proteções opcionais de take‑profit, stop‑loss e trailing stop podem ser ativadas.

A estratégia processa apenas **velas concluídas** e usa a API de alto nível do StockSharp com vinculação de indicadores.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `RsiPeriod` | Período de cálculo do RSI. | `14` |
| `LevelUp` | Nível de sobrecompra para acionar vendidos. | `70` |
| `LevelDown` | Nível de sobrevenda para acionar comprados. | `30` |
| `TakeProfitPercent` | Percentagem de take profit. `0` desativa. | `0` |
| `StopLossPercent` | Percentagem de stop loss. `0` desativa. | `0` |
| `TrailingStopPercent` | Percentagem de trailing stop. `0` desativa. | `0` |
| `CandleType` | Período das velas para cálculos. | `1 minuto` |

## Notas

O trailing stop usa o mecanismo integrado `StartProtection`. Quando `TrailingStopPercent` é maior que zero, substitui o stop loss regular e segue automaticamente o preço.
