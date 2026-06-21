# Estratégia de Momentum de Rendimento de Par Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera um par de divisas selecionado usando o momentum do diferencial de rendimento de 2 anos entre suas moedas. O momentum é medido como a diferença entre o diferencial e sua média móvel. As Bandas de Bollinger aplicadas ao momentum definem zonas de sobrecompra e sobrevenda. As posições são fechadas após um número fixo de barras.

## Características principais

- Usa o momentum do diferencial de rendimento de 2 anos para os sinais.
- As Bandas de Bollinger sobre o momentum identificam condições extremas.
- Inversão opcional da lógica de entrada.
- Fecha as posições após um número especificado de barras.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `YieldASecurity` | Primeiro ativo de rendimento. |
| `YieldBSecurity` | Segundo ativo de rendimento. |
| `CandleType` | Período de velas para análise. |
| `MomentumLength` | Período para a média do diferencial de rendimento. |
| `BollingerLength` | Período para as Bandas de Bollinger. |
| `BollingerStdDev` | Multiplicador de desvio padrão para as bandas. |
| `HoldPeriods` | Barras para manter uma posição. |
| `ReverseLogic` | Inverter as condições de comprado e vendido. |

## Complexidade

Iniciante

