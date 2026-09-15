# Alternância de tendência com trava de cooldown
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas médias exponenciais que se cruzam em uma série rápida de candles produzem muito mais sinais do que a tendência realmente muda, e uma sequência de cruzamentos em torno de um mesmo nível de preço pode encher a conta com entradas que se anulam. Este diagrama pega um cruzamento, gasta-o e então tranca aquele lado por um número fixo de candles. A trava é um bloco Flag, a contagem regressiva que a abre de novo é um bloco Delay value, e quem inicia a contagem é um Combination que funde as execuções das duas direções em uma única linha chamada "houve uma entrada".

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de uma única série alimentam uma média móvel exponencial rápida e uma lenta; nada no diagrama reage a um candle ainda em formação.
- Dois blocos Crossing leem o mesmo par de médias com as entradas invertidas, de modo que um deles é verdadeiro exatamente na barra em que a média rápida cruza acima da lenta e o outro exatamente na barra em que ela cruza abaixo.
- Um bloco Position comparado com zero informa se a conta está zerada, comprada ou vendida, e é essa resposta que transforma um cruzamento em um sinal permitido, e não em mera observação.
- Cada direção tem o seu Flag. O sinal de entrada é o gatilho dele, e um Flag só deixa o gatilho passar na primeira vez em que é acionado, de modo que o segundo sinal daquele lado e todos os seguintes são descartados em silêncio.
- As entradas são blocos Position modify que só abrem a partir de uma conta zerada, o que mantém em sincronia o sinal permitido e a ordem efetivamente registrada.
- Os dois blocos de entrada enviam suas execuções para um único Combination, e essa linha única arma o bloco Delay value, entrega a execução ao Position protection e desenha as execuções no gráfico.
- O Delay value conta os candles finalizados após a execução e emite um pulso quando a contagem termina; esse pulso está ligado ao conector de reset dos dois Flags, e ambos os lados voltam a operar.
- Um cruzamento contrário com a posição aberta é recolhido por um segundo Combination e encerra a operação, enquanto as distâncias de take-profit e stop-loss podem encerrá-la antes.

## Regras de entrada e saída

- **Entrada comprada**: A média rápida cruza acima da lenta em um candle finalizado enquanto a conta está zerada. A condição lógica que une esses dois fatos aciona o Flag de compra; se o Flag ainda estiver travado por uma compra anterior, o sinal morre ali e nada é enviado. Caso contrário, o Flag dispara uma vez, o Position modify compra a mercado com o volume configurado, e o Flag permanece acionado até que a contagem regressiva o libere.
- **Entrada vendida**: A média rápida cruza abaixo da lenta em um candle finalizado enquanto a conta está zerada. O sinal passa pelo Flag de venda sob a mesma regra e o Position modify vende a mercado com o mesmo volume. Os dois Flags são independentes, portanto um lado comprado travado não impede que uma venda seja aberta.
- **Saída**: Um cruzamento para baixo com posição comprada e um cruzamento para cima com posição vendida se encontram em um Combination que aciona um Position modify configurado para fechar, e ele mesmo calcula o volume de fechamento. O Position protection funciona em paralelo sobre a execução de entrada e pode encerrar a operação antes, na distância de take-profit ou na de stop-loss. Nada é invertido em uma única ordem: a posição volta primeiro a zero, e o lado oposto é aberto depois, por um cruzamento que encontre a conta vazia e aquele lado destravado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles sobre a qual funciona tudo o que há no diagrama. Ele também define a unidade do cooldown, que é contado em candles dessa série. |
| Fast EMA Length | 14 | Período da média móvel exponencial rápida. |
| Slow EMA Length | 40 | Período da média móvel exponencial lenta. Mantenha-o claramente acima do rápido; médias de períodos parecidos se cruzam o tempo todo e o latch acabaria fazendo toda a filtragem. |
| Cooldown Candles | 72 | Número de candles finalizados durante os quais um lado permanece travado após a execução de uma entrada. Aumentá-lo rareia as operações; reduzi-lo permite que um trecho lateral produza várias entradas seguidas. |
| Volume | 1 | Tamanho de uma entrada, em unidades do instrumento. As duas direções o utilizam. |
| Take Profit, % | 1.5 | Distância do take-profit, em porcentagem do preço de entrada. |
| Stop Loss, % | 1 | Distância do stop-loss, em porcentagem do preço de entrada. |

## Detalhes do diagrama

- Um Flag nunca emite falso. Ele é um latch, não um portão: informa o momento em que é acionado, e tudo o que bloqueia é bloqueado pelo silêncio — por isso quem reabre um lado é o conector de reset, e não um Not lógico.
- O Delay value só se arma a partir de um estado vazio. Uma execução que chegue com a contagem já em andamento não a prolonga, de modo que a pausa é medida a partir da primeira execução de uma sequência, e não da última.
- A contagem regressiva é conduzida pela própria série de candles: os candles entram na entrada de contagem e a decrementam, de modo que a pausa é expressa em barras e acompanha o time frame, em vez de um relógio.
- O pulso de reset abre os dois lados de uma vez, e cada lado é travado separadamente. Uma direção que não negociou durante a pausa é destravada por um pulso pago pela outra direção, e essa é a diferença deliberada entre um latch por lado e um único temporizador global.
- A condição de entrada exige uma conta zerada e o bloco de entrada se recusa de propósito a atuar a partir de qualquer outro estado: o latch é gasto pelo sinal, portanto um sinal que não pudesse ser executado desperdiçaria a pausa daquele lado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
