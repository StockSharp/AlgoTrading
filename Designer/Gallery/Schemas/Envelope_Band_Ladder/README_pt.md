# Diagrama da estratégia Envelope Band Ladder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia reversão à média nas Bandas de Bollinger em velas de cinco minutos, com uma escada de entrada de dois níveis, reversões limitadas, saídas na banda central e cancelamento programado de ordens pendentes.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de cinco minutos alimentam Bollinger Bands com período 20 e largura 1.5. Comparações estritas detectam fechamento abaixo da banda inferior ou acima da superior somente depois que o indicador está formado.
- As entradas são aceitas de 00:00:00 a 16:59:59 UTC, inclusive. Com posição zerada, o primeiro nível é uma ordem a mercado de uma unidade e o segundo é um limite pendente de uma unidade em `2 × lower − middle` para compra ou `2 × upper − middle` para venda.
- Um sinal contrário a uma posição de um ou dois níveis cancela o limite antigo e envia uma ordem a mercado de três unidades. Assim, qualquer exposição permitida passa para uma ou duas unidades na nova direção sem criar outro nível distante.
- O retorno pela banda central tem prioridade menor que uma entrada simultânea além da banda oposta. A saída cancela o nível pendente e encadeia duas ações ReduceOnly a mercado de uma unidade; a segunda atua apenas se ainda restar outra unidade.
- Fora da janela de entrada, um Flag diário ativa cancelamentos em massa e direcionados. A exposição aberta não é encerrada pelo horário; as saídas pela banda central permanecem ativas o dia todo.

## Regras de entrada e saída

- **Entrada comprada**: Na janela UTC, `Close < lower band` e `Position ≤ 0` formam um candidato de compra. Com posição zerada são enviados uma compra a mercado de uma unidade e um limite distante de uma unidade em `2 × lower − middle`; com posição vendida, limites antigos são cancelados e três unidades são compradas para reverter a posição limitada.
- **Entrada vendida**: Na janela UTC, `Close > upper band` e `Position ≥ 0` formam um candidato de venda. Com posição zerada são enviados uma venda a mercado de uma unidade e um limite superior de uma unidade em `2 × upper − middle`; com posição comprada, limites antigos são cancelados e três unidades são vendidas para reverter a posição limitada.
- **Saída**: Sem uma entrada contrária de maior prioridade, a posição comprada sai após `Close > middle band` e a vendida após `Close < middle band`. Os níveis pendentes são cancelados primeiro; duas ações ReduceOnly a mercado, encadeadas e de uma unidade, removem até dois níveis executados sem atravessar a posição zerada. O filtro de horário cancela ordens, mas não força a saída.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Período das velas concluídas usado por todos os cálculos de sinal. |
| Bollinger Length | 20 | Período retrospectivo do indicador Bollinger Bands já formado. |
| Bollinger Width | 1.5 | Número de desvios padrão usado nas bandas superior e inferior. |
| Entry Start | 00:00:00 UTC | Início UTC inclusivo da janela fixa de entrada. |
| Entry End | 16:59:59 UTC | Fim UTC inclusivo da janela fixa de entrada; depois dele os limites pendentes são cancelados. |
| Rung Volume | 1 | Quantidade de cada nível comum da escada e de cada etapa de saída ReduceOnly. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas concluídas de cinco minutos e pode construí-las a partir do histórico de um minuto incluído. Um bloco [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calcula as três linhas de Bollinger.
- Blocos [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html), [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) e [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) extraem Close e aplicam filtros estritos de banda, posição, sessão e prioridade.
- O bloco [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) é um filtro fixo do diagrama. Blocos Formula e Variable calculam e guardam os preços distantes e a quantidade tripla de reversão no instante da entrada.
- Seis blocos [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) cobrem entradas a mercado com posição zerada, reversões limitadas a mercado e os dois limites pendentes. Blocos [Mass order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html) marcam cada fronteira de cancelamento; dois cancelamentos direcionados guardam e cancelam os limites distantes ativos.
- Dois blocos [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) executam a saída ReduceOnly encadeada. O [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) mostra velas, as três bandas e o fluxo de negócios da estratégia.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
