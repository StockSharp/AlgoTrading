# Diagrama da estratégia de emulação Renko sem repintura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os traders recorrem aos gráficos de tijolos porque um tijolo, uma vez impresso, nunca muda: um sinal tomado sobre ele não pode ser desfeito um minuto depois. Este diagrama obtém a mesma propriedade a partir de candles de tempo comuns. A fonte de candles é deliberadamente assinada com atualizações intermediárias, e um bloco Final value fica entre ela e todo bloco que decide, de modo que uma média, um cruzamento e uma entrada são todos decididos sobre uma barra que já fechou. O fluxo ao vivo é mantido, mas vai apenas para o gráfico, onde um candle em formação tem o seu lugar.

![schema](schema.svg)

## Visão geral da estratégia

- Os candles de cinco minutos são assinados com atualizações intermediárias habilitadas, de modo que a fonte emite a barra muitas vezes enquanto ela se forma e mais uma vez quando ela fecha.
- Um bloco Final value do tipo candle é a única porta da fonte para a lógica: ele repassa um valor apenas quando esse valor é final, de modo que nada mais adiante jamais vê um preço que ainda pode se mover.
- Esse bloco é estrutural, não decorativo. Um bloco de indicador marca como final todo valor que lhe é entregue, portanto uma média alimentada com uma barra em formação reescreveria a leitura dessa mesma barra a cada atualização, e um cruzamento construído sobre duas médias assim apareceria e desapareceria dentro da barra.
- Uma média exponencial rápida, uma lenta e um índice de força relativa são todos construídos sobre o fluxo fechado, e os três estão configurados para se manifestar apenas depois de formados, de modo que as primeiras decisões esperam por uma janela lenta completa.
- Dois blocos Crossing carregam os dois lados do mesmo evento: um tem a média rápida na entrada up e a lenta na entrada down, o outro as tem trocadas, de modo que cada um dispara verdadeiro no cruzamento que lhe dá nome.
- O índice de força relativa é comparado com uma variável de linha média nos dois sentidos, dando um filtro de momentum que precisa concordar com o cruzamento antes que qualquer coisa seja enviada.
- A posição é lida para uma variável de retenção acionada por cada candle fechado e comparada com zero duas vezes, de modo que a entrada pode exigir uma posição que não esteja comprada e a saída pode exigir uma que esteja.
- Dois blocos lógicos AND reúnem cruzamento, momentum e posição; um aciona uma entrada a mercado com a condição Open position, o outro uma saída a mercado com a condição Close position, e o painel de gráfico desenha os candles ao vivo, os três indicadores, as ordens e as execuções.

## Regras de entrada e saída

- **Entrada comprada**: Em um candle fechado, a média rápida cruza acima da lenta, o índice de força relativa está acima de sua linha média e a posição não está comprada. O bloco AND reúne as três respostas e o bloco de entrada compra o volume da ordem a mercado. Sua condição Open position significa que a ordem só é enviada a partir de uma posição zerada, portanto um segundo cruzamento enquanto uma operação está em curso não muda nada.
- **Entrada vendida**: Não há entradas vendidas. Abaixo da linha média, ou com a média rápida abaixo da lenta, o diagrama simplesmente fica de fora; a única ordem que ele envia na direção de venda é a que encerra uma posição comprada.
- **Saída**: Em um candle fechado, a média rápida cruza de volta abaixo da lenta, o índice de força relativa caiu abaixo de sua linha média e a posição está comprada. O segundo bloco AND dispara e o bloco de fechamento vende toda a posição a mercado, com o volume calculado a partir da posição aberta pela condição Close position. Não há bloco de stop-loss nem de take-profit: o cruzamento que abriu a operação é a mesma coisa que a encerra, e cada uma dessas decisões é tomada sobre uma barra que já terminou.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles sobre a qual todo o diagrama funciona; a série é assinada com atualizações intermediárias, e o bloco Final value separa dela as barras fechadas. |
| Fast EMA Length | 14 | Período da média exponencial rápida, a mais veloz das duas linhas cujos cruzamentos são o sinal. |
| Slow EMA Length | 40 | Período da média exponencial lenta, a linha de referência contra a qual a rápida é medida. |
| RSI Length | 14 | Período do índice de força relativa usado como filtro de momentum nos dois lados. |
| RSI Midline | 50 | Nível que divide o filtro de momentum: acima dele o diagrama pode abrir uma compra, abaixo dele uma compra pode ser encerrada. |
| Order Volume | 1 | Tamanho da ordem enviada pela entrada; a saída, em vez disso, toma seu tamanho a partir da posição aberta. |

## Detalhes do diagrama

- O bloco lógico dispara exatamente na barra do cruzamento. Um bloco Crossing emite apenas no momento em que as duas linhas trocam de lugar, enquanto as comparações emitem a cada candle fechado; uma condição lógica retém cada entrada até que todas tenham chegado desde o último disparo, de modo que a peça que falta é sempre o cruzamento e as respostas com que ele é combinado são as daquela mesma barra.
- Os dois blocos Crossing são o mesmo bloco com as entradas trocadas. Cada um emite verdadeiro quando sua própria entrada up alcança ou supera sua entrada down e falso no evento oposto, razão pela qual um deles leva o nome do cruzamento de alta e o outro o do cruzamento de baixa, em vez de um único bloco alimentar os dois blocos lógicos.
- O bloco de posição só se manifesta quando a posição muda, o que em uma semana calma pode ser nunca. A variável de retenção atrás dele começa em zero, guarda o último número que recebeu e o reemite a cada candle fechado, de modo que ambas as comparações sempre têm um lado esquerdo atualizado com que trabalhar.
- Nada espaça os sinais no tempo. Todo cruzamento que se qualifica é aproveitado, e a única coisa que impede uma entrada de cair em cima de outra é a exigência de que a posição não esteja comprada, respaldada pela condição Open position no próprio bloco de ordem.
- O candle em formação não é descartado, apenas é mantido longe das decisões: o fluxo bruto é desenhado no gráfico ao lado dos indicadores, que são plotados a partir do fluxo fechado, de modo que os dois podem ser lidos um contra o outro.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
