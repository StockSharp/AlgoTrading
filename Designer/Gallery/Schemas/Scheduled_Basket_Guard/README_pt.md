# Cesta Agendada de Duas Pernas com Proteção Financeira
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia um relógio, não um sinal. Ele não contém nenhum indicador: uma vez por dia abre-se uma curta janela de entrada e o diagrama compra dois instrumentos no mesmo instante, uma janela posterior zera as duas pernas e, entre as duas janelas, uma proteção financeira acompanha o resultado da cesta e a encerra antecipadamente ao atingir o alvo de lucro ou o limite de perda.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos Variable do tipo Security nomeiam os dois instrumentos negociados pelo diagrama. Cada um alimenta a sua própria série de candles, a sua própria ação de abertura e a sua própria ação de fechamento, de modo que as duas pernas são dimensionadas e geridas separadamente, embora sejam abertas e fechadas juntas.
- O Sync retém as duas séries de candles de 5 minutos até que ambas as pernas tenham entregue a mesma barra e as libera como um único conjunto, de forma que o valor traçado para a cesta nunca misture um preço novo de uma perna com um preço defasado da outra.
- Um bloco Formula multiplica cada fechamento liberado pelo volume negociado da respectiva perna e soma os dois produtos. O resultado é quanto a cesta realmente vale nos volumes que o diagrama negocia, e é a linha desenhada no painel do gráfico.
- O Working time lê o carimbo de tempo dos candles finalizados da primeira perna e só fica aberto dentro da janela de entrada, de modo que exatamente uma barra por dia passa por ele. O Flag transforma essa abertura em um único pulso e permanece travado até que a cesta seja zerada.
- O pulso travado aciona dois blocos Position modify configurados como Open position. Cada um leva o seu próprio Security e o seu próprio Volume, e a condição Open position mantém o par silencioso sempre que aquele instrumento já estiver em carteira, de modo que um pulso nunca pode empilhar uma segunda cesta sobre a primeira.
- O Strategy P&L alimenta um bloco Formula que soma o resultado realizado e o resultado em aberto. Um segundo Formula subtrai o valor capturado no momento em que a cesta foi aberta, o que transforma o total acumulado da conta no resultado apenas da cesta atual.
- Um OR lógico reúne três motivos para zerar: a janela de fechamento, um resultado da cesta acima do alvo de lucro e um resultado da cesta abaixo do limite de perda. Seu sinal aciona dois blocos Position modify configurados como Close position e também libera o Flag diário, de modo que a próxima janela de entrada encontra a trava livre.

## Regras de entrada e saída

- **Entrada comprada**: Dentro da janela de entrada, o candle finalizado da primeira perna abre o Working time, o Flag diário ainda não foi usado e os dois blocos Open position disparam no mesmo pulso: um compra a primeira perna pelo seu próprio volume, o outro compra a segunda perna pelo seu próprio volume. Cada ordem é uma ordem a mercado e cada uma é suprimida no seu próprio instrumento se já houver uma posição aberta nele.
- **Entrada vendida**: O diagrama não tem lado vendido: os dois blocos de entrada trazem a direção Buy fixa. Uma cesta vendida está a um ajuste de distância — mude o Direction dos dois blocos Open position para Sell e o mesmo agendamento, a mesma trava e a mesma proteção financeira conduzem a cesta no sentido inverso.
- **Saída**: Três motivos zeram a cesta, e qualquer um deles basta. A janela de fechamento, guiada pelo relógio da estratégia e não pela chegada de candles, zera no horário; o resultado da cesta subindo acima do alvo de lucro zera antecipadamente com lucro; o resultado da cesta caindo abaixo do limite de perda zera antecipadamente com prejuízo. Os três passam por um único OR lógico até dois blocos Close position, que não precisam nem de volume nem de direção porque calculam ambos a partir da posição que encontram — e não fazem nada quando não há nenhuma, de modo que um sinal repetido dentro da janela é inofensivo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| First Leg Security | BTCUSDT@BNBFT | Instrumento negociado pela primeira perna; é ele que conduz a série de candles que temporiza a janela de entrada. |
| Second Leg Security | TONUSDT@BNBFT | Instrumento negociado pela segunda perna; é aberto e fechado nos mesmos sinais da primeira. |
| First Leg Candles | 00:05:00 | Time frame da primeira perna. Apenas candles finalizados são usados, portanto a janela de entrada precisa ter pelo menos a largura de um candle. |
| Second Leg Candles | 00:05:00 | Time frame da segunda perna. Mantenha-o igual ao da primeira perna, já que as duas são retidas juntas antes de o valor da cesta ser calculado. |
| Alignment Interval | 00:05:00 | Intervalo de agrupamento usado para alinhar as duas pernas. Deve coincidir com o time frame dos candles; um valor maior liberaria o par depois da barra à qual ele pertence. |
| Entry Window From | 10:00:00 | Início da janela diária de entrada, lido do carimbo de tempo dos candles finalizados da primeira perna. |
| Entry Window Until | 10:04:00 | Fim da janela diária de entrada. O intervalo entre os dois valores deve conter exatamente uma abertura de candle; caso contrário, a trava diária seria armada mais de uma vez. |
| Flatten Window From | 17:00:00 | Início da janela diária de zeramento, lido do relógio da estratégia e não da chegada de candles. |
| Flatten Window Until | 17:10:00 | Fim da janela diária de zeramento. Mantenha-a com a largura de alguns candles para que o relógio seja amostrado dentro dela pelo menos uma vez. |
| First Leg Size | 0.01 | Quantidade usada para abrir a primeira perna e o peso que a primeira perna tem no valor da cesta desenhado. |
| Second Leg Size | 100 | Quantidade usada para abrir a segunda perna e o peso que a segunda perna tem no valor da cesta desenhado. Escolha-a de modo que as duas pernas contribuam com montantes financeiros comparáveis. |
| Profit Target | 100 | Lucro da cesta atual acima do qual ela é encerrada antecipadamente. É medido a partir do momento em que a cesta foi aberta, não a partir do início da execução. |
| Loss Limit | -500 | Prejuízo da cesta atual abaixo do qual ela é encerrada antecipadamente; um valor negativo, medido da mesma forma que o alvo de lucro. |

## Detalhes do diagrama

- Dois blocos [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) do tipo Security são o único lugar em que um instrumento é nomeado. Cada um alimenta um bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) e a entrada Security dos dois blocos Position modify que gerenciam aquela perna, de modo que uma perna é redirecionada para outro instrumento editando um único valor.
- O [Sync](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html) recebe uma linha por perna e libera as duas juntas assim que a barra estiver completa em ambas. Os candles liberados são desenhados no painel do gráfico e convertidos em preços de fechamento, que um [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) pondera pelos volumes negociados para formar a linha de valor da cesta.
- Os dois relógios são deliberadamente diferentes. O [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) de entrada é alimentado pelos candles da primeira perna, de modo que a decisão de entrada não pode se afastar do preço em que é tomada; o Working time de fechamento é alimentado pelo [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html), de modo que o zeramento acontece mesmo quando os dados ficam parados. O [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) fica entre a janela de entrada e as ordens e só é reiniciado pelo sinal de zeramento, o que é justamente o que limita o diagrama a uma cesta por dia.
- O [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) informa o resultado realizado e o resultado em aberto a cada atualização. Somá-los dá o total acumulado da conta; um Variable captura esse total na primeira execução de entrada, um segundo Variable o mantém e o reemite a cada atualização posterior, e subtraí-lo deixa o resultado da cesta que está aberta agora. A proteção, portanto, mede a cesta atual e não toda a vida da conta, e volta a zero assim que a cesta é fechada.
- Toda ação é um bloco [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html). Os dois blocos de entrada usam a condição Open position com direção e volume explícitos; os dois blocos de saída usam Close position, que não recebe nenhum dos dois e deriva ambos da posição atual do seu próprio instrumento. O [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) funde os quatro fluxos de execuções na única série de negócios desenhada no painel do gráfico, enquanto os quatro fluxos de ordens são desenhados separadamente.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
