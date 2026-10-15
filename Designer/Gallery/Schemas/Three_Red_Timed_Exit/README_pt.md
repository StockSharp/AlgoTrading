# Diagrama da estratégia de três candles vermelhos com saída por tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia três candles consecutivos da mesma cor quando a volatilidade está elevada. Três candles vermelhos com ATR 14 acima de 0,8 vez a sua média de 30 valores formam uma condição comprada; três candles verdes sob a mesma regra formam uma condição vendida. A partir de posição zerada a entrada é direta, uma posição contrária é revertida em duas etapas confirmadas por execução e uma posição aberta pode fechar pelo padrão oposto ou após vinte barras concluídas. Cada sequência de ações inicia um cooldown de doze candles.

![schema](schema.svg)

## Visão geral da estratégia

- Somente candles concluídos de 30 minutos são processados. Dois blocos Previous value e seis Converter extraem Open e Close do candle atual e dos dois anteriores, portanto cada barra testa uma janela móvel de três candles vermelhos e verdes.
- ATR 14 e sua média simples de 30 valores precisam estar formados. A alta volatilidade é a condição estrita `ATR > média do ATR × 0,8`; não há decisão durante o aquecimento dos indicadores.
- Três candles vermelhos válidos compram a partir de zero ou revertem uma posição vendida. Três candles verdes válidos vendem a partir de zero ou revertem uma posição comprada. A reversão primeiro fecha uma unidade com ReduceOnly e só abre uma unidade na nova direção depois que a Order de fechamento estiver totalmente executada.
- Sem reversão válida de alta volatilidade, três candles verdes fecham uma posição comprada e três vermelhos fecham uma vendida. Um contador com estado também fecha qualquer lado após vinte barras concluídas de permanência; a reversão no mesmo candle tem prioridade sobre a saída temporal.
- Três blocos Combination reúnem os dois motivos de saída comprada, os dois de saída vendida e os fluxos de execução das oito ações. Uma execução bloqueia os doze candles concluídos seguintes; o décimo terceiro é o primeiro elegível. Não há stop-loss, take-profit nem bloco de proteção de posição.

## Regras de entrada e saída

- **Entrada comprada**: Quando o candle atual e os dois candles concluídos anteriores fecham abaixo das respectivas aberturas, ATR 14 está estritamente acima de `média do ATR × 0,8` e o cooldown está pronto, uma posição zerada envia uma compra a mercado NoCondition com Order Volume 1. A partir de uma posição vendida, primeiro é enviada uma compra a mercado ReduceOnly de 1; sua Order totalmente executada atualiza o volume e aciona a compra a mercado NoCondition de 1.
- **Entrada vendida**: Quando o candle atual e os dois candles concluídos anteriores fecham acima das respectivas aberturas, ATR 14 está estritamente acima de `média do ATR × 0,8` e o cooldown está pronto, uma posição zerada envia uma venda a mercado NoCondition com Order Volume 1. A partir de uma posição comprada, primeiro é enviada uma venda a mercado ReduceOnly de 1; sua Order totalmente executada atualiza o volume e aciona a venda a mercado NoCondition de 1.
- **Saída**: Uma posição comprada fecha com três candles verdes consecutivos quando o padrão não constitui simultaneamente uma reversão de alta volatilidade, ou quando Max Hold Bars chega a 20. Uma posição vendida fecha de modo simétrico com três candles vermelhos ou após 20 barras. Eventos de padrão e temporizador são reunidos por lado, e um Flag permite no máximo um fechamento independente por candle. Cada fechamento independente e ambas as execuções de uma reversão em etapas alimentam o cooldown comum.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles Series | 00:30:00 | Série de 30 minutos; somente candles concluídos atualizam padrões, indicadores, contadores, cooldown e decisões. |
| ATR Length | 14 | Período do indicador Average True Range que emite apenas valores formados. |
| ATR Average Length | 30 | Período da média móvel simples calculada sobre o ATR; as decisões aguardam a formação dessa média. |
| ATR Multiplier | 0.8 | Multiplicador da média do ATR. A volatilidade só é válida quando o ATR supera estritamente o limiar resultante. |
| Max Hold Bars | 20 | Número de barras concluídas contadas enquanto a posição não está zerada antes de liberar o fechamento temporal. |
| Cooldown Bars | 12 | Número de candles concluídos seguintes bloqueados após uma sequência de ações; as decisões retornam no candle 13. |
| Order Volume | 1 | Quantidade fixa para entradas a partir de zero, fechamentos ReduceOnly e entradas após fechamento executado; o diagrama é dimensionado para a posição de uma unidade criada por ele. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles concluídos de 30 minutos. Dois [Previous value](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) guardam os deslocamentos 1 e 2, e seis [Converter](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extraem os três pares Open/Close.
- Seis [Comparison](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) classificam cada candle com testes estritos `Close < Open` e `Close > Open`. Dois [Logical condition](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) de três entradas formam os padrões móveis vermelho e verde; um doji torna ambos falsos.
- Dois [Indicator](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) de valores formados calculam ATR 14 e SMA 30 do ATR. Um bloco [Formula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) multiplica a média por 0,8 e uma comparação estrita fornece o sinal de alta volatilidade.
- A [Position](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual é capturada duas vezes por candle: para rotear operações e para o contador de permanência. Blocos [Variable](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) e Formula incrementam o contador apenas com posição aberta e o zeram por execuções confirmadas.
- Dois [Combination](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/combination.html) booleanos reúnem saídas de padrão e temporizador sem contar ou alterar valores. Blocos [Flag](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/flag.html) por lado evitam fechamentos independentes duplicados, e portas de prioridade os suprimem quando o mesmo candle já atende a uma reversão.
- Oito blocos [Modify position](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) implementam duas entradas a partir de zero, duas saídas independentes e duas reversões em etapas. Cada reversão usa `ReduceOnly close 1 → fully matched Order → NoCondition open 1`; a Order executada também reemite o volume no mesmo ciclo.
- Uma Combination MyTrade envia cada execução de ação ao cooldown [N values](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html). A primeira execução inicia a contagem de doze candles, enquanto a segunda etapa da mesma reversão não reinicia uma contagem ativa. O [Chart panel](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe candles, ATR, média, limiar e todas as execuções.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
