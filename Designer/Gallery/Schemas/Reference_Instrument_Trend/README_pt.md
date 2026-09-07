# Diagrama da estratégia com tendência do instrumento de referência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama primeiro sincroniza candles finalizados de cinco minutos de BTCUSDT@BNBFT e TONUSDT@BNBFT e depois negocia cruzamentos exatos das EMAs de BTC quando a tendência EMA de TON confirma a mesma direção. Filtros de posição e pausa controlam cada decisão, e dois caminhos de ordens a mercado com volume fixo gerenciam a exposição.

![schema](schema.svg)

## Visão geral da estratégia

- O bloco Sync recebe os dois fluxos de candles finalizados de cinco minutos com Interval `00:05:00` e ClearSockets ativado. Ele só emite um par BTC–TON alinhado quando ambos os candles estão presentes; se faltar um deles, o intervalo incompleto é descartado.
- Cada par alinhado alimenta depois a EMA rápida 7 e a EMA lenta 18 de BTC, e a EMA rápida 47 e a EMA lenta 50 de TON. A filtragem exclusiva de valores formados está desativada nos quatro indicadores.
- Um cruzamento de alta do BTC exige `PrevFast <= PrevSlow` e `Fast > Slow`; um cruzamento de baixa exige `PrevFast >= PrevSlow` e `Fast < Slow`. A relação atual do TON confirma compras com `Fast > Slow` e vendas com `Fast < Slow`.
- O caminho de compra também exige `Position <= 0`, enquanto o caminho de venda exige `Position >= 0`. Ambos enviam ordens a mercado `NoCondition` com Volume fixo de 1.
- Os primeiros cinco pares sincronizados BTC–TON ficam bloqueados, e cada sinal de ordem bloqueia os cinco pares sincronizados seguintes; o sexto par alinhado volta a ser elegível. Não há stop-loss, take-profit nem bloco separado de saída, e o gráfico mostra candles de BTC, as duas EMAs de BTC e os dois fluxos de execuções.

## Regras de entrada e saída

- **Entrada comprada**: Quando o BTC satisfaz `PrevFast <= PrevSlow` e `Fast > Slow`, o TON apresenta `Fast > Slow`, a verificação sincronizada é `Position <= 0` e a pausa terminou, o diagrama envia uma compra a mercado com Volume 1.
- **Entrada vendida**: Quando o BTC satisfaz `PrevFast >= PrevSlow` e `Fast < Slow`, o TON apresenta `Fast < Slow`, a verificação sincronizada é `Position >= 0` e a pausa terminou, o diagrama envia uma venda a mercado com Volume 1.
- **Saída**: Não há bloco dedicado de saída ou proteção. Uma ordem posterior elegível na direção contrária reduz a exposição; a partir de uma posição `+1` ou `-1`, o Volume fixo de 1 leva a posição a zero em vez de abrir o lado oposto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Main Fast EMA | 7 | Período da ExponentialMovingAverage rápida calculada com candles finalizados de cinco minutos de BTCUSDT@BNBFT; o filtro exclusivo de valores formados está desativado. |
| Main Slow EMA | 18 | Período da ExponentialMovingAverage lenta calculada com candles finalizados de cinco minutos de BTCUSDT@BNBFT; o filtro exclusivo de valores formados está desativado. |
| Reference Fast EMA | 47 | Período da ExponentialMovingAverage rápida calculada com o candle finalizado e alinhado de cinco minutos de TONUSDT@BNBFT; o filtro exclusivo de valores formados está desativado. |
| Reference Slow EMA | 50 | Período da ExponentialMovingAverage lenta calculada com o candle finalizado e alinhado de cinco minutos de TONUSDT@BNBFT; o filtro exclusivo de valores formados está desativado. |
| Cooldown Bars | 5 | Número de pares de candles sincronizados iniciais e posteriores a um sinal que ficam bloqueados antes de liberar o próximo par alinhado. |
| Volume | 1 | Quantidade fixa fornecida aos dois blocos de ordens a mercado NoCondition. |

## Detalhes do diagrama

- Dois blocos [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) enviam candles finalizados de cinco minutos de BTCUSDT@BNBFT e TONUSDT@BNBFT diretamente à sincronização.
- O bloco [Sync](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/sync.html) alinha as duas entradas de candles com Interval `00:05:00` e ClearSockets `true`. Ele libera ambos os candles como um par; se faltar qualquer lado, o intervalo incompleto é limpo sem entrar na cadeia de indicadores.
- Somente o par sincronizado alimenta quatro blocos de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html): ExponentialMovingAverage 7 e 18 para BTC e 47 e 50 para TON. A opção exclusiva de valores formados é `false` em todos, e somente as duas saídas EMA de BTC também seguem para o gráfico.
- Blocos de [Valor anterior](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) mantêm os valores sincronizados anteriores das EMAs rápida e lenta de BTC. Blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) expressam os dois lados de cada cruzamento exato e as duas relações atuais de tendência do TON; caminhos separados de [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) os combinam para compra e venda.
- A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual fornece as verificações `Position <= 0` e `Position >= 0`. A porta [N values](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) recebe a saída sincronizada de candles BTC e usa N=5 para suprimir os primeiros cinco pares alinhados e os cinco posteriores a cada sinal de ordem, reativando as decisões no sexto.
- Os blocos de compra e venda [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocam ordens a mercado com `NoCondition` e Volume 1 compartilhado. Não há stop-loss, take-profit, proteção de posição nem elemento separado de saída.
- O [Painel do gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe candles finalizados de BTC, BTC EMA 7, BTC EMA 18 e as saídas MyTrade dos blocos de compra e venda.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
