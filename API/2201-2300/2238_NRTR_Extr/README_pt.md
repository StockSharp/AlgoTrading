# Estratégia NRTR Extr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o algoritmo **Nick Rypock Trailing Reverse** (NRTR) com setas de sinal adicionais. É uma conversão do exemplo original do MQL5 "Exp_NRTR_extr" para a API de alto nível do StockSharp.

## Como funciona

- O `NrtrExtrIndicator` personalizado calcula uma faixa média durante um período configurável e desenha um nível de trailing que segue o preço.
- Quando o preço reverte além desse nível, o indicador muda de direção e emite um sinal de compra ou venda.
- A estratégia abre uma posição comprada em um sinal de compra e uma posição vendida em um sinal de venda.
- As posições existentes são fechadas no sinal oposto ou quando os níveis definidos de stop loss ou take profit são atingidos.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Period` | Número de velas usadas para o cálculo da faixa média. |
| `Digits Shift` | Ajuste de precisão adicional aplicado ao fator de faixa. |
| `Stop Loss` | Stop de proteção em pontos de preço. |
| `Take Profit` | Alvo de lucro em pontos de preço. |
| `Enable Buy Open` / `Enable Sell Open` | Permitir abertura de posições compradas ou vendidas. |
| `Enable Buy Close` / `Enable Sell Close` | Permitir fechamento de posições existentes em sinais opostos. |
| `Candle Type` | Período de tempo das velas usadas para o indicador. |

## Notas

O indicador é baseado no Average True Range para estimar a volatilidade do mercado. Para visualização, a estratégia desenha automaticamente velas e operações executadas na área do gráfico.

