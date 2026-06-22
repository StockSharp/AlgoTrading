# Estratégia RoBoost
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação em C# do assessor especialista MQL4 original **RoBoostj**.
Opera em um único instrumento usando sinais baseados em RSI combinados com detecção
simples de momentum de preço. A estratégia opera no tipo de vela selecionado
(padrão: velas de 1 hora).

## Lógica

- Quando o preço de fechamento anterior é maior que o fechamento atual e o valor do RSI
  cai abaixo do limiar **RSI Down**, a estratégia abre uma posição vendida.
- Quando o preço de fechamento anterior é menor ou igual ao fechamento atual e o valor
  do RSI sobe acima do limiar **RSI Up**, a estratégia abre uma posição comprada.
- As posições ativas são gerenciadas com as seguintes ferramentas de risco:
  - Níveis fixos de **Take Profit** e **Stop Loss** medidos em unidades de preço.
  - Trailing stop opcional ativado quando a operação avança no lucro pela distância
    **Trail Start**. Após a ativação, o preço de stop segue o preço pela distância **Trail Step**.

## Parâmetros

| Nome            | Descrição                                                     |
|-----------------|---------------------------------------------------------------|
| `CandleType`    | Série de velas usada para os cálculos.                        |
| `RsiPeriod`     | Comprimento do período do indicador RSI.                      |
| `RsiUp`         | Limiar RSI usado para entradas compradas.                     |
| `RsiDown`       | Limiar RSI usado para entradas vendidas.                      |
| `TakeProfit`    | Distância de take profit do preço de entrada (pontos).        |
| `StopLoss`      | Distância de stop loss do preço de entrada (pontos).          |
| `UseTrailing`   | Ativa a lógica de trailing stop.                              |
| `TrailStart`    | Distância em pontos para ativar o trailing stop.              |
| `TrailStep`     | Distância em pontos mantida do preço atual quando o
                   trailing stop está ativo.                                       |

Todas as distâncias são expressas em unidades de preço absolutas e podem exigir ajuste
de acordo com o tamanho do tick do instrumento.

## Uso

1. Adicione a estratégia ao seu projeto ou abra-a no StockSharp Designer.
2. Configure os parâmetros de acordo com suas preferências de trading.
3. Inicie a estratégia. Ela se inscreverá automaticamente na série de velas escolhida
   e gerenciará as operações com base nos valores de RSI e fechamentos de velas.

A estratégia é destinada a fins educativos e deve ser testada em dados históricos
antes de usar em mercados ao vivo.
