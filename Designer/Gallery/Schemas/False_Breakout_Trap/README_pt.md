# Diagrama da estratégia de armadilha de falso rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama negocia o retorno ao intervalo anterior de vinte velas depois de o preço ultrapassar brevemente uma das fronteiras. Dois bloqueios por direção limitam entradas repetidas, enquanto uma SMA fornece saídas imediatas para a posição aberta.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de um minuto são separadas nos fluxos High, Low e Close.
- Highest(20) e Lowest(20), cada um seguido por Previous value com Shift = 1, definem um intervalo que exclui a vela atual.
- High acima da máxima anterior com Close de volta abaixo dela é um falso rompimento para cima; a condição espelhada em Low vale para baixo.
- O Flag de venda e o Flag de compra compartilham um N values de 500 velas concluídas; cada lado libera um evento antes da próxima redefinição comum.
- Entradas a mercado usam volume fixo de um quando a posição está zerada, e as condições da SMA(20) reduzem a posição pelo mesmo volume.

## Regras de entrada e saída

- **Entrada comprada**: Low fica abaixo da mínima das vinte velas anteriores, Close retorna acima dessa fronteira e o bloqueio de compra aceita o evento. Comprar uma unidade a mercado apenas com posição zerada.
- **Entrada vendida**: High fica acima da máxima das vinte velas anteriores, Close retorna abaixo dessa fronteira e o bloqueio de venda aceita o evento. Vender uma unidade a mercado apenas com posição zerada.
- **Saída**: Reduzir uma posição comprada em uma unidade quando Close estiver abaixo da SMA(20), ou reduzir uma posição vendida em uma unidade quando Close estiver acima da SMA(20). As saídas são imediatas e não passam pela pausa de entradas. O diagrama não tem stop-loss nem take-profit.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles Series | 00:01:00 | Velas concluídas de um minuto usadas em todos os cálculos de intervalo, sinal e saída. |
| Highest Length | 20 | Quantidade de máximas de vela na fronteira superior móvel. |
| Highest Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; Candle high está ligado diretamente. |
| Lowest Length | 20 | Quantidade de mínimas de vela na fronteira inferior móvel. |
| Lowest Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; Candle low está ligado diretamente. |
| SMA Length | 20 | Quantidade de fechamentos na média móvel usada nas saídas. |
| SMA Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; Candle close está ligado diretamente. |
| Cooldown N | 500 | Quantidade de velas concluídas consumidas antes da redefinição comum da pausa. |
| Entry Volume | 1 | Volume fixo a mercado para cada entrada e saída redutora. |

## Detalhes do diagrama

- Highest e Lowest recebem valores numéricos High e Low, enquanto SMA recebe Close; os três indicadores emitem apenas valores formados.
- Previous value desloca os dois indicadores de intervalo em uma atualização, portanto a vela testada não participa da própria fronteira.
- O pulso final de avaliação chega às duas portas AND depois da atualização dos campos da vela, indicadores, posição e comparações.
- Cada evento não filtrado de falso rompimento arma o N values compartilhado. Sua saída redefine os dois Flags após 500 velas concluídas seguintes; até lá cada Flag suprime repetições do seu lado.
- OpenPosition impede uma nova ordem enquanto há posição. O evento ainda pode armar a pausa porque esse ramo fica antes da ação de posição.
- As duas saídas pela SMA verificam o sinal da posição e usam ações a mercado ReduceOnly de uma unidade. Chart recebe velas, as duas fronteiras anteriores, SMA e todas as execuções.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
