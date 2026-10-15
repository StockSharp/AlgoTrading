# Estratégia Universal do Investidor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão direta do consultor especialista **Universal Investor** MetaTrader 4. Ele combina uma média móvel exponencial (EMA) e uma média móvel ponderada linear (LWMA) para confirmar a direção da tendência de curto prazo e realiza negociações de uma posição com dimensionamento de posição adaptativo.

## Lógica de negociação

1. Assine o `CandleType` configurado e calcule EMA e LWMA com o período definido por `MovingPeriod`.
2. Armazene os dois valores mais recentes de cada média móvel para que a lógica imite as chamadas `iMA(..., shift = 1/2)` do EA original.
3. Gere um sinal de **compra** quando o LWMA anterior estiver acima do EMA anterior, ambas as médias estiverem subindo e não houver sinal oposto na mesma vela.
4. Gere um sinal de **venda** quando o LWMA anterior estiver abaixo do EMA anterior, ambas as médias estiverem caindo e não houver sinal oposto na mesma vela.
5. Feche uma posição longa aberta assim que o LWMA cair abaixo de EMA (lógica de espelho para vendas).
6. Calcule o volume de negociação a partir do parâmetro da estratégia `Volume`, aumente-o para satisfazer o requisito `MaximumRisk` quando o valor do portfólio for grande o suficiente e reduza-o após perdas consecutivas em negociações de acordo com `DecreaseFactor`.
7. Envie ordens de mercado com `BuyMarket`/`SellMarket` e acompanhe o preço de entrada para detectar saídas vencedoras ou perdedoras.

A estratégia mantém apenas uma posição aberta por vez e reverte imediatamente somente após um fechamento total, reproduzindo o comportamento do script MetaTrader original.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de velas usadas para cálculos. |
| `MovingPeriod` | Período para EMA e LWMA. |
| `MaximumRisk` | Fração do patrimônio líquido (0,05 = 5%) utilizada para calcular o volume mínimo da posição. |
| `DecreaseFactor` | Reduz o volume após negociações consecutivas perdidas (0 desativa o recurso). |
| `Volume` | Volume base do contrato passado para `BuyMarket`/`SellMarket`. |

## Indicadores

- `ExponentialMovingAverage`
- `LinearWeightedMovingAverage`

## Notas

- Os pedidos são feitos apenas em velas fechadas, correspondendo ao EA que depende de verificações de `Time[0]`.
- A lógica do tamanho da posição reflete a função MetaTrader `LotsOptimized`, incluindo o componente baseado em risco e o multiplicador de sequência de perdas.
