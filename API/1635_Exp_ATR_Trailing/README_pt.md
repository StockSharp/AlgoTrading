# Estratégia Exp de Trailing ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo demonstra como gerenciar posições existentes com um trailing stop baseado no indicador **Average True Range (ATR)**. A estratégia não gera sinais de entrada; ela apenas ajusta o nível de saída de uma posição aberta de acordo com a volatilidade do mercado.

## Como funciona

1. A estratégia se inscreve em dados de velas de um período escolhido.
2. Um indicador `AverageTrueRange` é calculado em cada vela.
3. Para posições compradas, o nível de stop é movido para cima para `Close - ATR * BuyFactor`.
4. Para posições vendidas, o nível de stop é movido para baixo para `Close + ATR * SellFactor`.
5. Quando o preço cruza o nível de trailing, a posição é fechada a mercado.

O trailing stop apenas se move na direção do trade e nunca recua, fornecendo uma saída ajustada à volatilidade.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `AtrPeriod` | Período de cálculo do ATR. |
| `BuyFactor` | Multiplicador aplicado ao ATR ao fazer trailing de uma posição comprada. |
| `SellFactor` | Multiplicador aplicado ao ATR ao fazer trailing de uma posição vendida. |
| `CandleType` | Período das velas usadas para análise. |

## Notas de uso

- Anexar a estratégia a um instrumento e abrir uma posição manualmente ou a partir de outra estratégia.
- Adequada para gestão de risco onde as saídas são controladas separadamente das entradas.
- A área do gráfico exibe velas, valores de ATR e negociações executadas para análise visual.

## Referências

- [Average True Range na documentação do StockSharp](https://doc.stocksharp.com/topics/indicator_average_true_range.html)
- [Strategy Designer](https://doc.stocksharp.com/topics/designer.html)
