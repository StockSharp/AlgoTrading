# Diagrama da estratégia Fractal Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um fractal é um candle cuja máxima é a maior de cinco, ou cuja mínima é a menor de cinco, e só pode ser identificado dois candles depois de ter acontecido. O diagrama mantém esse atraso honesto: trabalha apenas com candles finalizados, transforma cada fractal em um preço de stop e deixa esse preço se mover em uma única direção. Cada rompimento de um stop inverte a posição, e o nível rompido fica de lado até que um novo fractal o rearme.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles alimenta tudo, e ela está configurada para entregar somente candles finalizados, de modo que nenhum nível e nenhuma ordem é construído a partir de um preço que um tick posterior ainda poderia desfazer.
- Highest e Lowest tomam o extremo dos últimos cinco candles, enquanto Previous value devolve o candle de dois candles atrás e dois conversores leem sua máxima e sua mínima.
- Quando a máxima de dois candles atrás é a maior da janela, aquele candle é um fractal superior; o teste espelhado na mínima marca um fractal inferior. Uma variável com trava armazena o preço de cada fractal e o libera apenas no candle em que o teste foi aprovado.
- Uma fórmula soma o percentual de buffer ao preço do fractal superior e o subtrai do inferior, transformando um fractal em um preço de stop.
- Mais duas fórmulas travam esses preços com min e max: o stop superior só pode cair e o stop inferior só pode subir, e é isso que os torna trailing stops em vez de simples níveis de topos e fundos.
- O fechamento de cada candle finalizado é comparado com os dois stops: acima do stop superior o diagrama se inverte para comprado, abaixo do stop inferior ele se inverte para vendido.
- Uma inversão é um par de blocos, um fechando o que está aberto e outro abrindo o novo lado, e o stop que acabou de ser usado fica estacionado fora de alcance enquanto durar a posição que ele abriu.
- Os candles, os dois níveis de stop e cada execução são desenhados em uma mesma área do gráfico, de modo que a trava progressiva pode ser lida diretamente na figura.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento de um candle finalizado sobe acima do trailing stop superior. Position modify no modo Close recompra a posição vendida, se houver uma aberta, e Position modify no modo Open compra o volume da ordem assim que a conta fica zerada.
- **Entrada vendida**: O fechamento de um candle finalizado cai abaixo do trailing stop inferior enquanto o preço ainda está abaixo do superior. O mesmo par funciona no sentido inverso: a posição comprada é fechada primeiro e depois o volume da ordem é vendido.
- **Saída**: Não há regra de saída separada. Uma posição é mantida até que o stop oposto seja rompido, e esse rompimento ao mesmo tempo a fecha e abre o lado contrário, de modo que o diagrama está sempre comprado, vendido ou a uma execução disso. O trailing stop se aperta conforme novos fractais aparecem, e é isso que move o preço de saída atrás de uma operação aberta.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da única série de candles. Somente candles finalizados são entregues, portanto uma decisão é tomada uma vez por candle. |
| Upper Fractal Length | 5 | Número de candles sobre os quais o extremo superior é medido. O candle sob teste fica no meio dessa janela. |
| Lower Fractal Length | 5 | A mesma janela para o extremo inferior; mantenha-a igual à superior, ou os dois lados lerão fractais de larguras diferentes. |
| Fractal Shift | 2 | A que distância no passado fica o candle sob teste. Ele tem de ser o centro da janela: dois para uma janela de cinco, três para uma janela de sete. |
| Stop Buffer, % | 0 | Percentual somado ao preço do fractal superior e subtraído do inferior antes de o nível ser usado. Zero coloca o stop exatamente sobre o fractal; um valor maior o mantém um pouco mais afastado do preço. |
| Order Volume | 1 | Tamanho da ordem, em lotes. O mesmo tamanho é usado para os dois lados, então uma inversão é o fechamento do tamanho antigo seguido da abertura deste. |

## Detalhes do diagrama

- Candles finalizados são o que impede os níveis de serem repintados. O bloco do indicador trata todo valor entregue a ele como definitivo, então um candle ainda em formação seria gravado em Highest e Lowest e depois sobrescrito na sua atualização seguinte; entregar somente candles finalizados significa que os dois indicadores nunca veem um valor que possa mudar.
- O teste do fractal usa 'maior ou igual' em vez de 'maior', de modo que um topo plano em que dois candles compartilham a mesma máxima ainda conta como fractal, e o mesmo vale para um fundo duplo do lado da mínima.
- Por construção, um fractal só é identificado dois candles depois, e o diagrama não tenta esconder isso: o nível que ele produz é o que era verdadeiro dois candles atrás, e é usado a partir do candle em que se torna conhecido.
- Enquanto uma posição está aberta, o stop que a abriu fica estacionado bem fora de alcance e se rearma a partir do primeiro fractal que se forma depois que a posição deixa de existir. Sem isso, o diagrama continuaria sinalizando o lado em que já está, e a trava progressiva arrastaria o nível para tão longe do preço que ele nunca mais poderia ser cruzado.
- Caso os dois stops apareçam como rompidos no mesmo instante, o lado comprado tem precedência: a perna vendida carrega a condição adicional de que o preço ainda esteja abaixo do stop superior, de modo que os dois nunca podem disparar juntos.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
