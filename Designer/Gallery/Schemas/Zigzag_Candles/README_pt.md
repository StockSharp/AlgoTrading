# Diagrama da estratégia Zigzag Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama mantém dois níveis de swing sempre ativos — a máxima mais alta e a mínima mais baixa dos últimos cinco candles horários finalizados — e age sobre o candle que atinge um deles. Um par de blocos Flag que se reiniciam mutuamente permite uma única ação por swing, de modo que um nível tocado repetidas vezes dentro do mesmo movimento produz uma ordem e nada além disso. Cada swing primeiro zera o que estiver aberto; o swing seguinte abre o lado oposto.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles horários alimenta tudo e entrega apenas candles finalizados, de modo que nenhum nível e nenhuma ordem são construídos a partir de um preço que um tick posterior ainda poderia desfazer.
- Dois conversores leem a máxima e a mínima de cada candle e alimentam um Highest de cinco e um Lowest de cinco. Ambos os indicadores publicam somente depois de formados, portanto nenhum nível existe antes de cinco candles terem fechado.
- Dois blocos Variable guardam os níveis. Cada um recebe o valor mais recente do indicador sem liberá-lo e republica o que armazena a cada candle, de modo que o nível permanece disponível às comparações em todas as barras, e não apenas na barra que o alterou.
- Mais dois conversores leem a máxima e a mínima do candle recém-fechado, e dois blocos Comparison as confrontam com os níveis armazenados: uma máxima que atinge ou supera o nível superior, uma mínima que atinge ou perfura o nível inferior.
- Um retrato de Position é retido uma vez por candle e comparado com zero duas vezes, produzindo um teste de não vendido e um teste de não comprado. Cada um dos dois rompimentos é combinado com o teste correspondente por um Logical condition AND, de modo que o nível superior só é acionado enquanto a posição ainda não estiver vendida, e o nível inferior somente enquanto ela ainda não estiver comprada.
- Cada AND aciona o Trigger de um Flag, e o rompimento oposto aciona o Reset desse Flag. Um Flag deixa passar um único true e depois se cala até ser reiniciado, o que transforma um nível tocado muitas vezes dentro de um mesmo swing em exatamente uma ação.
- Um Flag que dispara alcança ao mesmo tempo um Modify position configurado como Close position e um Modify position configurado como Open position. Apenas um deles se aplica: uma posição existente é zerada a mercado, uma posição zerada é aberta no lado que o swing pede, e o bloco que não se aplica rejeita o sinal.
- O painel de gráfico desenha os candles horários, as duas linhas de extremos, os três fluxos de ordens e cada execução, de modo que os níveis de swing e as ações tomadas sobre eles podem ser lidos em uma única imagem.

## Regras de entrada e saída

- **Entrada comprada**: A mínima de um candle finalizado atinge ou cai abaixo do nível inferior armazenado enquanto a posição não está comprada. O Flag inferior dispara uma vez: uma posição vendida é recomprada a mercado e fica zerada, e uma posição zerada é convertida em uma compra de Order Volume. Depois disso o Flag permanece calado, por mais vezes que a mínima seja revisitada, até que o nível superior seja atingido.
- **Entrada vendida**: A máxima de um candle finalizado atinge ou supera o nível superior armazenado enquanto a posição não está vendida. O Flag superior dispara uma vez: uma posição comprada é vendida a mercado e fica zerada, e uma posição zerada é convertida em uma venda de Order Volume. Esse Flag permanece calado até que o nível inferior seja atingido.
- **Saída**: Não há stop, não há alvo e não há temporizador - a posição é fechada pelo swing oposto. O mesmo Flag que abre um lado também alimenta o bloco Close position, portanto o primeiro toque no outro nível zera o que estiver aberto. Como o volume de entrada é fixo e o bloco de abertura só aceita uma posição zerada, uma reversão completa sempre exige dois swings: um para zerar e o seguinte para abrir o outro lado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 01:00:00 | Time frame da série de candles. Somente candles finalizados são entregues, portanto uma decisão é tomada uma vez por barra. |
| Swing High Length | 5 | Quantidade de candles finalizados sobre a qual o extremo superior é calculado. Uma janela mais longa torna o nível superior mais difícil de alcançar e os swings mais longos; uma mais curta transforma quase todo candle em um novo extremo. |
| Swing Low Length | 5 | Quantidade de candles finalizados sobre a qual o extremo inferior é calculado. É mantida separada da superior para que os dois lados possam ser deliberadamente assimétricos. |
| Initial Swing High | 999999999 | Valor que o nível superior mantém até o indicador superior estar formado. É deliberadamente inatingível, para que nenhuma máxima possa tocá-lo durante o aquecimento e nenhuma venda seja aberta antes de existir um extremo real. |
| Initial Swing Low | 0 | Valor que o nível inferior mantém até o indicador inferior estar formado. Zero não pode ser alcançado por cima por um preço negociado, o que mantém o lado comprado quieto pelo mesmo motivo. |
| Order Volume | 1 | Quantidade usada pelas duas ações de abertura. A ação de fechamento não precisa de volume - ela zera o que estiver aberto -, portanto, com uma quantidade fixa, a posição só transita entre um lote vendido, zerada e um lote comprado. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) está inscrito apenas em candles finalizados, e é isso que torna os níveis honestos. Os indicadores são alimentados através de [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html), e um conversor emite um número puro, que sempre conta como final; um candle ainda em formação entraria, portanto, nas janelas do Highest e do Lowest como se estivesse completo, e o extremo acabaria contendo a si mesmo. A mesma assinatura é o que mantém as ordens válidas: uma ordem construída a partir da atualização de um candle não finalizado carrega o horário de abertura daquela barra e é recusada por chegar do passado.
- Um rompimento é medido contra os níveis tal como estavam antes do candle que está sendo avaliado: as variáveis de retenção republicam o que já traziam, e os indicadores absorvem o novo candle na mesma barra, de modo que o nível contra o qual um candle é testado é o extremo dos cinco candles que vieram antes dele, e não um que inclua a si mesmo.
- Os dois blocos [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) estão ligados de forma cruzada: o rompimento superior reinicia o Flag inferior e o rompimento inferior reinicia o superior. Esse par é toda a memória do diagrama - ele diz para que lado foi o último swing e se já houve ação sobre ele. Um Flag ignora um false em qualquer das entradas, portanto as comparações que publicam false a cada atualização de candle não custam nada, e ele volta a emitir apenas depois que o nível oposto tiver sido atingido.
- Os níveis são guardados por blocos [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) cuja entrada não funciona como gatilho; quem aciona o Trigger deles é a série de candles. O mesmo padrão retém o valor de [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) uma vez por candle, de modo que as duas [Comparisons](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) de posição e os dois blocos [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND leem todos o mesmo retrato, e não um valor que se desloca sob eles.
- Três blocos [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) atuam, todos a mercado: um com Close position que recebe os dois Flags, e um com Open position para cada lado. Em um candle de abrangência, que ao mesmo tempo supera a máxima anterior e perfura a mínima anterior, os dois Flags podem disparar na mesma passagem e duas ordens a mercado são enviadas; elas se anulam na posição, mas ambas aparecem como execuções no log e no [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html).

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
