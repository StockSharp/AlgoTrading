# Diagrama da estratégia Twenty Pips Once a Day
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama assume no máximo uma posição contra a tendência por dia. Uma vez ao dia, em uma hora escolhida do relógio e somente enquanto a conta está zerada, ele compara o fechamento do candle horário concluído com o fechamento do candle de 29 barras antes e aposta contra a deriva dessa janela: compra depois de uma queda e vende depois de uma alta. Um take-profit pequeno, um stop mais largo e um limite rígido de idade da posição cuidam da saída.

![schema](schema.svg)

## Visão geral da estratégia

- Candles horários concluídos comandam tudo. Nada é avaliado dentro de uma barra em formação, portanto cada decisão é tomada sobre um preço já fechado.
- O Previous value guarda o candle de 29 barras atrás. Seu fechamento é comparado com o fechamento atual, o que mede a deriva de aproximadamente o último dia e um quarto.
- A comparação decide o lado contra essa deriva: um fechamento antigo acima do atual significa que o mercado caiu e o esquema compra; um fechamento antigo abaixo dele significa que o mercado subiu e o esquema vende. São usadas duas comparações estritas, de modo que uma janela que termina exatamente onde começou não produz sinal algum.
- O Time fornece a leitura do relógio que acompanha o candle recém-fechado, um conversor extrai a hora dele e uma comparação com o parâmetro Trading Hour abre a janela de entrada por um candle ao dia.
- A posição atual precisa estar zerada. Junto com o filtro de hora uma vez ao dia e a condição Open position nos blocos de entrada, é isso que mantém o esquema em uma única posição por vez.
- Ambas as entradas são ordens a mercado de volume fixo. Suas execuções são unidas e entregues ao Position protection, que fecha a posição em um take-profit de 0.1% ou em um stop-loss de 0.5%, a mesma proporção de um para cinco sobre a qual a ideia é construída.
- Um contador N values é armado pela entrada aceita e conta 21 candles concluídos. Quando ele se esgota, um bloco Position modify configurado como Close position zera o que ainda estiver aberto, de modo que uma posição que não alcançou nenhum dos alvos não é carregada indefinidamente.
- O Is trade allowed observa a permissão de negociação ao vivo da plataforma. A cada entrada aceita o esquema registra qual era essa permissão naquele momento e escreve uma linha no log, o que é um relatório e não um veto: em uma reprodução histórica a permissão nunca é concedida, portanto condicionar a entrada a ela silenciaria todo o diagrama.

## Regras de entrada e saída

- **Entrada comprada**: Em um candle horário concluído cuja hora do relógio seja igual a Trading Hour, com a posição zerada e o fechamento de 29 barras atrás acima do fechamento atual, comprar Volume a mercado sob a condição Open position.
- **Entrada vendida**: Em um candle horário concluído cuja hora do relógio seja igual a Trading Hour, com a posição zerada e o fechamento de 29 barras atrás abaixo do fechamento atual, vender Volume a mercado sob a condição Open position.
- **Saída**: O Position protection fecha a posição em um take-profit de 0.1% ou em um stop-loss de 0.5% medidos a partir da execução de entrada, com o fechamento do candle alimentando suas verificações de preço. Se nenhum dos níveis for alcançado, o contador N values dispara 21 candles concluídos depois da entrada e o bloco Close position zera o restante; quando a proteção já fechou a posição, essa ação não encontra nada para fechar e não faz nada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 01:00:00 | Time frame dos candles de trabalho. Apenas candles concluídos são processados, portanto uma ordem nunca pode ser datada dentro de uma barra ainda em formação. |
| Lookback Bars | 29 | De quantas barras atrás é tomado o fechamento de referência. Essa é a largura da janela cuja deriva a entrada contraria. |
| Trading Hour | 7 | Hora do relógio em que a janela de entrada diária abre, lida do horário da estratégia que acompanha o candle concluído. |
| Volume | 0.1 | Quantidade fixa de ambas as ordens de entrada. Não há dimensionamento adaptativo: toda entrada tem o mesmo tamanho. |
| Max Position Bars | 21 | Quantos candles concluídos uma posição pode viver antes de ser zerada, independentemente de lucro ou prejuízo. |
| Take Profit % | 0.1 | Movimento favorável no qual o Position protection fecha a posição, como percentual do preço de entrada. |
| Stop Loss % | 0.5 | Movimento contrário no qual o Position protection fecha a posição, como percentual do preço de entrada. O stop é fixo, não é móvel. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) está configurado apenas para candles concluídos, e o [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) é aplicado ao próprio candle e não a um preço, com um [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) depois dele. Dois blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) transformam os dois fechamentos no lado comprado e no lado vendido; como ambos são estritos, uma janela inalterada não produz nenhum dos dois.
- Aqui o [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) é uma fonte de dados, não um rótulo: um conversor lê o seu Hour e uma comparação o confronta com uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html). A hora e a verificação de posição zerada estão ambas ancoradas ao candle, porque as constantes com as quais são comparadas são disparadas pelo fluxo de candles, de modo que o portão de entrada só pode se completar uma vez por barra concluída.
- A [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) e uma comparação com zero fornecem a verificação de posição zerada, e os dois blocos [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) reúnem deriva, hora e posição em um sinal por lado. Ambos os blocos [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) trazem a condição Open position, que é a segunda proteção contra uma entrada repetida enquanto uma posição está aberta.
- O sinal aceito também arma o [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html), que conta candles concluídos e então dispara um terceiro bloco Position modify configurado como Close position. Esse bloco não recebe volume: a quantidade a fechar é derivada da posição aberta, e uma conta zerada simplesmente não gera ordem.
- O [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) une as execuções dos dois lados de entrada para o [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Em paralelo, um [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) libera exatamente um pulso por entrada e é reiniciado pelo contador de idade; esse pulso trava a leitura do [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) em uma variável, que um bloco [String format](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) transforma em uma linha de log de [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) por posição assumida.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
