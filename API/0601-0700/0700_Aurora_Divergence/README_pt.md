# Estratégia de Divergência Aurora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera divergências entre o preço e o On-Balance Volume (OBV). Compara as inclinações de regressão linear do preço e do OBV para detectar possíveis reversões.

## Características principais

- Comparação de inclinações de regressão linear para sinais de divergência.
- Filtro z-score opcional para evitar preços sobreestendidos.
- Filtro de média móvel em período superior para confirmação de tendência.
- Limiar de volatilidade baseado em ATR e gestão de risco com stop e alvo dinâmicos.
- Resfriamento após cada operação e número máximo de barras em posição.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleType` | Período de candles para os cálculos principais. |
| `Lookback` | Período para cálculos de inclinação. |
| `ZLength` | Período para média e desvio padrão no filtro z-score. |
| `ZThreshold` | Z-score absoluto máximo para permitir entradas. |
| `UseZFilter` | Ativar ou desativar o filtro z-score. |
| `HtfCandleType` | Período superior para a média móvel de tendência. |
| `HtfMaLength` | Comprimento da média móvel no período superior. |
| `AtrLength` | Período ATR para volatilidade e risco. |
| `AtrThreshold` | Valor mínimo de ATR para permitir operações. |
| `StopAtrMultiplier` | Multiplicador ATR para a distância do stop-loss. |
| `ProfitAtrMultiplier` | Multiplicador ATR para a distância do take-profit. |
| `MaxBarsInTrade` | Número máximo de barras para manter uma posição. |
| `CooldownBars` | Barras de espera após uma operação antes de sinalizar novamente. |

## Complexidade

Intermediário
