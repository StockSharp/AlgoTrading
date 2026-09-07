# Diagrama de estratégia de sinais direcionais dois de três
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama avalia três votos direcionais em cada candle concluído de 30 minutos: a inclinação da linha Signal do MACD, a zona de %K do Stochastic e a zona do RSI. Qualquer par concordante produz um único evento de maioria para esse candle. Quando o cooldown está pronto, uma posição zerada é aberta com volume 1, enquanto uma posição oposta é revertida por meio de um fechamento ReduceOnly seguido de uma entrada na nova direção confirmada pela execução. Cada execução de uma nova posição inicia um cooldown de dez candles.

![schema](schema.svg)

## Visão geral da estratégia

- Candles concluídos de 30 minutos alimentam os indicadores MACD 12/26/9, Stochastic 14/3 e RSI 14, configurados para emitir somente quando estiverem formados. Um bloco Previous value mantém o valor precedente da linha Signal do MACD, e um gate de histórico impede uma decisão até que esse valor exista.
- Os votos de compra são `MACD Signal > Previous MACD Signal`, `Stochastic %K ≤ 20` e `RSI < 40`. Os votos de venda são `MACD Signal < Previous MACD Signal`, `Stochastic %K ≥ 80` e `RSI > 60`. Valores iguais do MACD e valores dos osciladores fora das zonas direcionais são neutros.
- Três blocos de Condição lógica em pares representam todas as maiorias possíveis de dois votos para cada direção. Um Flag por candle deixa passar somente o primeiro par satisfeito, de modo que três indicadores concordantes ainda criem um único evento direcional, e não três.
- Snapshots de posição e cooldown encaminham cada maioria. Com o cooldown pronto, uma posição zerada envia uma entrada NoCondition a mercado com Order Volume 1. Com uma posição oposta, primeiro é enviado um fechamento ReduceOnly a mercado de 1; somente a Order de fechamento totalmente executada inicia a entrada NoCondition a mercado de 1 na nova direção.
- Um bloco Combination reúne as execuções das quatro ações de nova posição em um único fluxo de cooldown. Uma execução marca o diagrama como indisponível para entradas, os dez candles concluídos seguintes são ignorados e o décimo primeiro candle concluído é o primeiro apto para uma decisão. Não há saída independente nem blocos de stop-loss, take-profit ou proteção de posição.

## Regras de entrada e saída

- **Entrada comprada**: Quando quaisquer dois votos de compra concordam e o cooldown está pronto, uma posição zerada envia uma compra NoCondition a mercado com Order Volume 1. Se a posição estiver vendida, o diagrama envia primeiro uma compra ReduceOnly a mercado de 1; somente a sua Order de fechamento totalmente executada aciona a compra NoCondition a mercado de 1. Uma posição comprada existente permanece inalterada.
- **Entrada vendida**: Quando quaisquer dois votos de venda concordam e o cooldown está pronto, uma posição zerada envia uma venda NoCondition a mercado com Order Volume 1. Se a posição estiver comprada, o diagrama envia primeiro uma venda ReduceOnly a mercado de 1; somente a sua Order de fechamento totalmente executada aciona a venda NoCondition a mercado de 1. Uma posição vendida existente permanece inalterada.
- **Saída**: Não existe uma regra de saída separada. Uma maioria na direção oposta fecha o lado atual e depois abre o novo lado por meio da sequência em etapas. O fechamento ReduceOnly não pode aumentar a exposição, mas ambas as etapas usam o Order Volume fixo de 1; a sequência é dimensionada para a posição de uma unidade criada pelo diagrama, e um tamanho real de posição diferente pode não ser totalmente fechado nem terminar na direção sinalizada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles Series | 00:30:00 | Série de candles de trinta minutos; somente candles concluídos atualizam os indicadores, redefinem os Flags de maioria por candle, avançam o cooldown e iniciam decisões. |
| MACD Fast Length | 12 | Fixo no bloco Indicator do MACD; edite esse bloco para alterar o período da EMA rápida. |
| MACD Slow Length | 26 | Fixo no bloco Indicator do MACD; edite esse bloco para alterar o período da EMA lenta. |
| MACD Signal Length | 9 | Fixo no bloco Indicator do MACD; edite esse bloco para alterar o período da EMA Signal, cuja inclinação de um candle fornece o voto do MACD. |
| Stochastic K Length | 14 | Fixo no bloco Indicator do Stochastic; edite esse bloco para alterar o período de %K. Os limites de compra e venda são 20 e 80. |
| Stochastic D Length | 3 | Fixo no bloco Indicator do Stochastic; edite esse bloco para alterar o período de %D. O voto direcional lê %K, e o indicador completo deve estar formado. |
| RSI Length | 14 | Período do RSI. Os limites direcionais fixos são estritamente abaixo de 40 e estritamente acima de 60. |
| Cooldown Bars | 10 | Número de candles concluídos subsequentes bloqueados após a execução de uma nova posição; as decisões são retomadas no candle 11. |
| Order Volume | 1 | Quantidade fixa usada pelas entradas a partir de posição zerada, pelas ações de fechamento ReduceOnly e pelas entradas confirmadas por execução após um fechamento. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite somente candles concluídos de 30 minutos. Três blocos [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), configurados para emitir somente quando estiverem formados, calculam MACD 12/26/9, Stochastic 14/3 e RSI 14.
- Um [Conversor](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extrai a linha Signal do MACD. Um bloco [Previous value](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) mantém seu valor de um candle atrás, e comparações estritas classificam a Signal atual como ascendente, descendente ou inalterada.
- Outros blocos [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) implementam os limites fixos exatos dos osciladores: Stochastic %K usa `≤ 20` e `≥ 80`, enquanto o RSI usa `< 40` e `> 60`. Os limites e os dois votos exigidos são configurações fixas do diagrama, e não parâmetros expostos.
- Seis blocos [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) em pares cobrem os três pares de compra possíveis e os três pares de venda possíveis. Dois blocos [Flag](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/flag.html) são redefinidos a cada candle e reduzem vários pares satisfeitos a um único evento de maioria por direção.
- A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual e o estado de cooldown pronto são capturados para o ciclo do candle. As comparações de posição distinguem exposição zerada, comprada e vendida, e as condições de entrada exigem tanto um evento de maioria quanto um cooldown disponível.
- Seis blocos [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) implementam duas entradas a partir de posição zerada e duas reversões em etapas. As rotas a partir de zero são protegidas externamente por `Position = 0`; os blocos de fechamento da reversão usam ReduceOnly, e suas saídas Order totalmente executadas acionam as entradas NoCondition fixas na direção oposta.
- Um bloco [Combination](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/combination.html) reúne imediatamente as saídas MyTrade dos quatro blocos de nova posição, sem contá-las nem alterá-las. A execução reunida desativa a disponibilidade de entrada e aciona um bloco [N values](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html), que conta dez candles concluídos subsequentes antes de restaurar a disponibilidade para o candle 11. O [Painel do gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe os candles, três fluxos numéricos de sinais, as execuções reunidas de novas posições e ambas as execuções de fechamento das reversões.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
