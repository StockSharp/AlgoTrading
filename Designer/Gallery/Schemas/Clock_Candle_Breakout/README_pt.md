# Diagrama da estratégia Clock Candle Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama escolhe um candle de referência por dia pelo relógio, memoriza sua máxima e sua mínima e opera o rompimento desses dois níveis durante os três candles finalizados seguintes. Um filtro de EMA 20 decide qual lado do rompimento pode ser operado, a janela fecha a posição quando expira, e um cooldown de dez candles mantém o diagrama fora do mercado por um tempo após cada entrada aceita.

![schema](schema.svg)

## Visão geral da estratégia

- Os candles de trinta minutos são entregues tanto enquanto ainda estão em formação quanto depois de finalizados. Um bloco Final value divide esse fluxo: o nível de referência, o contador da janela e o contador do cooldown veem apenas candles finalizados, enquanto o teste de rompimento lê o fechamento do candle que está em formação.
- O Working time marca o candle de referência: o candle finalizado cujo horário de abertura cai entre 02:30:00 e 02:59:59. Em um timeframe de trinta minutos exatamente um candle por dia se qualifica, e o intervalo de meia hora deixa margem para alterar o timeframe sem perder o pulso diário.
- Dois pares de blocos Variable extraem o nível desse candle. Em cada par, a primeira variável armazena a máxima (ou a mínima) de todo candle finalizado e só a libera quando chega o pulso do Working time; a segunda guarda o que foi liberado e o repete a cada atualização de candle, de modo que o nível permanece na linha entre um candle de referência e o seguinte.
- O mesmo pulso arma um contador N values ajustado em três. Ele conta candles finalizados e dispara assim que o terceiro candle após o candle de referência fecha, e é isso que encerra a janela de negociação.
- O estado da janela é uma única Variable numérica escrita através de uma Combination a partir de três origens: um quando o candle de referência é tomado, zero quando o contador de três candles dispara e zero assim que uma entrada é aceita. Uma Comparison contra zero transforma esse número no portão que ambos os ramos de entrada leem, de modo que uma janela produz no máximo uma posição.
- Uma compra exige o fechamento acima da máxima de referência e acima da EMA 20; uma venda exige o fechamento abaixo da mínima de referência e abaixo da EMA 20. Cada lado é um bloco Logical condition AND que também exige janela aberta, cooldown decorrido e posição zerada.
- Uma entrada aceita envia uma ordem a mercado através de Modify position com a condição Open position, de modo que um sinal que se repita dentro do mesmo candle não possa empilhar uma segunda ordem sobre a primeira. O mesmo sinal arma um segundo contador N values de dez candles finalizados; um segundo par de Variable, unido por sua própria Combination, mantém o flag do cooldown em zero até essa contagem se esgotar e o restaura para um em seguida.
- Quando o contador de três candles dispara, dois blocos Modify position com a condição Close position o recebem. Aquele cujo lado se opõe à posição aberta a zera a mercado; o outro não tem nada a fechar e rejeita o sinal.

## Regras de entrada e saída

- **Entrada comprada**: Durante os três candles finalizados que se seguem ao candle de referência, com o cooldown decorrido e a posição zerada, um fechamento acima da máxima de referência e acima da EMA 20 envia uma compra a mercado de Order Volume através de Open position.
- **Entrada vendida**: Durante esses mesmos três candles, com o cooldown decorrido e a posição zerada, um fechamento abaixo da mínima de referência e abaixo da EMA 20 envia uma venda a mercado de Order Volume através de Open position.
- **Saída**: A posição é fechada pelo tempo, não pelo preço: quando o contador da janela de três candles dispara, os blocos Close position zeram a mercado o que estiver aberto. Não há stop, nem alvo, nem regra de trailing, portanto o tempo de permanência nunca excede a janela, e a entrada aceita também escreve zero na janela, de modo que a mesma janela não pode ser operada duas vezes.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:30:00 | Timeframe da série de candles. São entregues tanto candles em formação quanto finalizados; apenas os finalizados definem o nível de referência e movimentam os dois contadores. |
| EMA Length | 20 | Período da média móvel exponencial que decide qual lado do rompimento pode ser operado. Os valores só são publicados depois que a média está formada, portanto nenhuma entrada é possível antes disso. |
| Reference From | 02:30:00 | Início do intervalo diário em que o candle de referência é procurado, lido a partir do horário de abertura de cada candle finalizado. |
| Reference Until | 02:59:59 | Fim desse intervalo. Junto com o início, ele deve cobrir exatamente uma abertura de candle por dia; o par padrão abrange um candle de trinta minutos. |
| Window Bars | 3 | Número de candles finalizados que a janela de negociação dura após o candle de referência. O mesmo contador fecha a posição quando ela expira. |
| Cooldown Bars | 10 | Número de candles finalizados contados após uma entrada aceita antes que o diagrama tenha permissão para operar de novo. |
| Order Volume | 1 | Quantidade fixa usada pelas duas ações Open position. As ações Close position não precisam de volume, porque zeram o que estiver aberto. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) publica igualmente candles em formação e finalizados, e o [Final value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/final_value.html) é o que os separa. Três [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) leem High e Low depois do Final value e Close antes dele, e é por isso que o nível vem sempre de um candle concluído enquanto o rompimento é testado contra um preço ao vivo.
- O [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) lê o horário de abertura do candle finalizado que lhe é enviado, de modo que sua saída é verdadeira para um candle por dia, e não para um intervalo de tempo de relógio. A ordem importa no diagrama: os conversores de High e Low estão ligados antes dele, portanto as [Variables](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) de armazenamento já contêm o candle atual quando o pulso as libera.
- Cada um dos dois contadores [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) é armado por um sinal e conta candles finalizados: três para a janela de negociação, dez para o cooldown. Suas saídas encontram os valores de abertura e de bloqueio em dois blocos [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html), e cada Combination alimenta uma Variable de estado que republica seu número a cada atualização de candle.
- O bloco [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) fornece a EMA 20. Sete blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) constroem os dois testes de rompimento, os dois testes de tendência, o portão da janela, o portão do cooldown e a verificação de posição zerada contra um instantâneo de [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) mantido por candle; dois blocos [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND os reúnem nos dois ramos de entrada, e um bloco OR transforma qualquer um dos ramos no sinal único que inicia o cooldown.
- Quatro blocos [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) atuam: dois com Open position para as entradas e dois com Close position para a saída por tempo. Cada execução é reunida por uma Combination e desenhada, junto com os candles, a EMA 20, ambos os níveis de referência e os quatro fluxos de ordens, no [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html).

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
