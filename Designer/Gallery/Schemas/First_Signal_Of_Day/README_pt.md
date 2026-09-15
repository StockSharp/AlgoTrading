# Diagrama da estratégia Primeiro Sinal do Dia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas médias exponenciais se cruzando em candles de cinco minutos são um sinal comum e, em um time frame rápido, ele se repete muitas vezes por dia. Este diagrama negocia apenas o primeiro sinal de cada direção por dia de calendário e deixa passar intocado todo cruzamento posterior. A memória que torna isso possível são dois blocos Flag, e o que limpa essa memória é o relógio da estratégia percebendo que a data mudou.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos finalizados alimentam uma média móvel exponencial rápida e uma lenta, e um bloco Crossing transforma o par em um único evento: ele emite true quando a média rápida cruza acima da lenta e false quando cruza abaixo.
- Um Not lógico transforma esse mesmo evento em um sinal separado de cruzamento para baixo, de modo que um único bloco Crossing atende às duas direções sem uma segunda cópia das médias.
- O bloco Current time transmite o relógio da estratégia para um conversor que extrai dele o dia do calendário, de modo que o diagrama passa a ter um número de dia que não depende em nada da série de candles.
- Uma variável guarda o número do dia e o libera quando um candle fecha, o que significa que ela sempre carrega o dia a que pertencia o candle anterior; uma comparação NotEqual contra o número de dia atual é, portanto, verdadeira exatamente uma vez, no primeiro candle após a meia-noite.
- Cada direção tem seu próprio Flag: o sinal de entrada é seu gatilho, e esse pulso de mudança de dia é seu reset. Um Flag deixa passar um gatilho apenas na primeira vez em que é acionado, de modo que tudo o que vem depois do primeiro sinal aceito do dia é descartado silenciosamente até que a data mude.
- As entradas são blocos Position modify configurados para abrir somente a partir de posição zerada, de modo que os dois travamentos consomem no máximo uma entrada comprada e uma vendida por dia e nunca acumulam volume.
- O cruzamento oposto fecha o que estiver aberto: duas condições lógicas se encontram em um Combination que aciona um único Position modify configurado para fechar, de modo que nenhum dos lados precisa de um bloco de saída próprio.
- O Position protection acompanha as execuções de entrada e pode encerrar a operação mais cedo a uma distância fixa de take-profit ou de stop-loss, de modo que a posição nunca fica esperando por um cruzamento que não vem.

## Regras de entrada e saída

- **Entrada comprada**: A média rápida cruza acima da lenta enquanto a posição está zerada. Essa combinação aciona o Flag de compra e, como um Flag dispara apenas em seu primeiro acionamento, o cruzamento só é executado se nenhuma posição comprada tiver sido aberta desde a última mudança de data. O Position modify então compra a mercado com o volume configurado, e recusa a ordem de imediato se algo já estiver aberto.
- **Entrada vendida**: A média rápida cruza abaixo da lenta enquanto a posição está zerada. O sinal passa pelo Flag de venda, que da mesma forma deixa passar apenas seu primeiro acionamento do dia, e o Position modify vende a mercado com o mesmo volume. Os dois Flags são independentes, de modo que um dia pode conter uma entrada comprada e uma vendida, em qualquer ordem.
- **Saída**: Um cruzamento para baixo estando comprado e um cruzamento para cima estando vendido são reunidos por um Combination que aciona um único Position modify configurado para fechar a posição, o qual calcula sozinho o volume de fechamento. O Position protection trabalha em paralelo sobre as execuções de entrada e pode fechar a operação mais cedo na distância de take-profit ou de stop-loss. Nada é revertido em um único passo: a posição primeiro volta a zero, e o lado oposto é aberto depois, em um cruzamento que encontra a conta vazia.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles sobre a qual todo o diagrama funciona. Uma série mais lenta significa menos cruzamentos por dia e um limite diário que raramente entra em ação. |
| Fast EMA Length | 14 | Período da média móvel exponencial rápida. |
| Slow EMA Length | 40 | Período da média móvel exponencial lenta. Mantenha-o confortavelmente acima do rápido; caso contrário, as duas médias se cruzam constantemente e, de qualquer forma, apenas o primeiro cruzamento de cada dia sobrevive ao travamento. |
| Volume | 1 | Tamanho de uma entrada, em unidades do instrumento. As duas direções o utilizam. |
| Take Profit, % | 1.5 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 1 | Distância do stop-loss, em percentual do preço de entrada. |

## Detalhes do diagrama

- O número do dia vem do relógio, e não dos candles, de modo que o reset continua funcionando em uma sessão com lacunas e não depende da existência de um candle no momento em que a data vira.
- O relógio corre em seu próprio ritmo, muito mais vezes do que os candles fecham. É por isso que seu valor não é comparado diretamente com um valor armazenado: uma variável primeiro o coloca no compasso dos candles, e é isso que faz a comparação significar 'este candle pertence a um dia diferente do candle anterior'.
- Um Flag ignora um false que chegue em qualquer um dos soquetes, de modo que a comparação de mudança de dia pode emitir false o dia inteiro sem perturbar o travamento, e uma condição lógica que resulta em false não custa nada.
- O filtro de entrada exige posição zerada e o próprio bloco de entrada se recusa a trabalhar a partir de qualquer outra situação, e isso é proposital: o travamento é consumido pelo sinal, então um sinal que não pudesse ser executado queimaria o dia para aquela direção.
- As duas direções compartilham uma única variável de volume e um único bloco de proteção, de modo que compra e venda são dimensionadas e protegidas exatamente pela mesma regra.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
