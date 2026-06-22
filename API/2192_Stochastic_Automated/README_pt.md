# Estratégia Stochastic Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera usando o **Oscilador Stochastic** no período de velas selecionado. Ela aguarda %K e %D entrarem em zonas extremas e então age nos cruzamentos para abrir posições. Take profit e stop loss fixos protegem cada operação, enquanto um trailing stop assegura os lucros.

## Lógica

1. **Entrada**
   - **Comprado:**
     - Tanto %K quanto %D estavam abaixo de `OverSold` há duas velas.
     - %D estava acima de %K há duas velas e abaixo de %K há uma vela.
     - %D está subindo.
   - **Vendido:**
     - Tanto %K quanto %D estavam acima de `OverBought` há duas velas.
     - %D estava abaixo de %K há duas velas e acima de %K há uma vela.
     - %D está caindo.
2. **Saída**
   - A posição é fechada quando o Stochastic sai da zona extrema ou %D vira na direção oposta.
   - Um trailing stop sai se o preço recuar em `TrailingStop`.
   - `TakeProfit` e `StopLoss` globais são aplicados a cada operação.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleType` | Período para os cálculos do Stochastic. |
| `KPeriod` | Período de retrospecto para a linha %K. |
| `DPeriod` | Período de suavização para a linha %D. |
| `Slowing` | Suavização adicional para %K. |
| `OverBought` | Limiar superior indicando mercado sobrecomprado. |
| `OverSold` | Limiar inferior indicando mercado sobrevendido. |
| `TakeProfit` | Distância da entrada para o alvo de lucro (unidades de preço). |
| `StopLoss` | Distância da entrada para o stop de proteção (unidades de preço). |
| `TrailingStop` | Distância de rastreamento quando a operação se move no lucro (unidades de preço). |

## Indicadores

- `StochasticOscillator`

## Notas

- Os comentários no código estão em inglês.
- A versão em Python é intencionalmente omitida.
