# Estratégia de Eficiência Fractal Polarizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia com base no indicador **Polarized Fractal Efficiency (PFE)**. O PFE mede a eficiência do movimento de preços e muda de sinal quando o momentum muda.

## Lógica de negociação

1. Assinar velas do período selecionado e calcular o PFE.
2. Se o PFE na barra anterior for menor que duas barras atrás e o valor atual for maior que o anterior, uma posição comprada é aberta.
3. Se o PFE na barra anterior for maior que duas barras atrás e o valor atual for menor que o anterior, uma posição vendida é aberta.
4. Posições opostas são fechadas antes de abrir novas.
5. Proteção opcional de stop loss e take profit é habilitada.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleType` | Série de velas usada para análise. |
| `PfePeriod` | Período para calcular o indicador PFE. |
| `SignalBar` | Deslocamento da barra usada para gerar sinais. |
| `TakeProfit` | Take profit em passos de preço. |
| `StopLoss` | Stop loss em passos de preço. |

