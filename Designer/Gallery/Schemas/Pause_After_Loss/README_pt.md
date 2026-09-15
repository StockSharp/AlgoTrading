# Diagrama da estratégia de pausa após perda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama de momentum simples carrega uma regra que decide quando é permitido negociar. O bloco P&L change informa o resultado realizado da conta, de modo que cada operação encerrada pode ser lida como ganho ou perda sem medir preços. As perdas que vêm uma após a outra são contadas e, quando a contagem atinge seu limite, um bloco Flag é acionado e mantém a negociação bloqueada por um número fixo de candles. As entradas só voltam depois que a contagem regressiva termina e o flag é limpo.

![schema](schema.svg)

## Visão geral da estratégia

- Candles horários finalizados alimentam um Rate of Change de um período, que é a variação percentual do preço de fechamento em relação ao fechamento anterior, de forma que um único indicador carrega tanto o limiar de entrada quanto o de saída.
- Duas comparações leem esse percentual contra duas variáveis: um limiar superior para um movimento de alta e outro inferior, negativo, para um movimento de baixa.
- O bloco Position é comparado com zero três vezes — igual, maior e menor —, o que fornece um teste de posição zerada para as entradas e um teste comprado e outro vendido para as saídas.
- Uma entrada é um E lógico de três sinais: momentum na direção desejada, posição zerada e nenhuma pausa em andamento. Ambos os blocos de entrada estão configurados apenas para abrir, portanto o diagrama mantém uma posição por vez e nunca acrescenta a ela.
- Uma saída é um E lógico entre momentum na direção oposta e uma posição desse lado; ela dispara um bloco Position modify configurado para encerrar, que retira o volume daquilo que está em carteira.
- O bloco P&L change informa o resultado realizado. Um bloco Previous value guarda o número que vigorava antes da última mudança, e duas comparações dizem se o resultado caiu ou subiu, ou seja, se a operação encerrada foi perdedora ou vencedora.
- Uma perda faz a sequência armazenada passar por uma fórmula que soma um e grava a soma de volta na mesma variável; um ganho grava zero por cima. A comparação da nova contagem com o limite é o sinal que inicia uma pausa.
- Esse sinal aciona um Flag e arma um bloco N values que conta candles finalizados. Enquanto o flag está acionado, uma variável de estado armazenada, lida a cada candle, informa "em pausa", um NÃO lógico a transforma em "liberado novamente", e essa é a terceira entrada de ambos os filtros de entrada.

## Regras de entrada e saída

- **Entrada comprada**: O Rate of Change do candle finalizado está acima do limiar de compra, a posição está zerada e não há pausa em andamento. O Position modify compra o volume da ordem a mercado, apenas abrindo posição.
- **Entrada vendida**: O Rate of Change do candle finalizado está abaixo do limiar de venda, a posição está zerada e não há pausa em andamento. O Position modify vende o volume da ordem a mercado, apenas abrindo posição.
- **Saída**: Uma posição é abandonada assim que o momentum se volta contra ela: uma compra é encerrada quando o Rate of Change cai abaixo do limiar de venda, e uma venda quando ele sobe acima do limiar de compra. O bloco está configurado para encerrar posição, portanto o volume da ordem vem da própria posição e o diagrama nunca inverte de um lado para o outro em uma única ordem — o lado oposto só pode ser aberto por um candle posterior, a partir da posição zerada. As saídas nunca são retidas pela pausa; apenas as entradas são.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 01:00:00 | Time frame dos candles com que todo o diagrama trabalha. A pausa também é contada nesses candles, portanto um candle mais longo torna a pausa mais longa em tempo de relógio. |
| Rate Of Change Length | 1 | Quantos candles para trás o Rate of Change mede. Em um, é a variação percentual em relação ao fechamento anterior, que é a base sobre a qual ambos os limiares foram escritos; um comprimento maior o transforma em uma medida de momentum mais ampla e os limiares precisam ser alargados junto. |
| Long Threshold, % | 0.3 | Movimento percentual que abre uma compra e encerra uma venda. Elevá-lo torna ambos mais raros e o diagrama mais seletivo. |
| Short Threshold, % | -0.3 | Movimento percentual que abre uma venda e encerra uma compra, escrito como número negativo. Ele não precisa espelhar o limiar de compra; valores assimétricos inclinam o diagrama para um dos lados. |
| Order Volume | 1 | Tamanho de cada ordem de entrada, em unidades do instrumento. As saídas tomam seu volume da posição, portanto esse valor não se repete ali. |
| Consecutive Losses | 3 | Quantas operações encerradas seguidas precisam dar prejuízo antes de a negociação ser suspensa. Uma operação vencedora devolve a contagem a zero, portanto isso conta uma sequência e não um total; em um, toda operação perdedora inicia uma pausa. |
| Pause Candles | 8 | Quantos candles finalizados dura uma pausa. As entradas são recusadas durante toda a contagem, após a qual o flag é limpo e a contagem de perdas volta a zero. |

## Detalhes do diagrama

- Nada do lado da negociação alimenta a pausa: o contador de sequência e o flag são conduzidos apenas pelo resultado da conta, portanto o diagrama não tem laço e a pausa só pode retirar permissão, nunca concedê-la.
- O bloco Flag emite apenas no momento em que é acionado pela primeira vez, e o bloco N values ignora um disparo enquanto já está contando, assim um novo sinal de perda durante uma pausa em andamento não reinicia nem prolonga a contagem regressiva.
- A contagem regressiva é medida em candles finalizados do time frame de negociação, e não em eventos da conta, de forma que um trecho calmo e outro agitado produzem uma pausa do mesmo tamanho.
- Os filtros de entrada leem a pausa a partir de uma variável armazenada, e não do próprio flag. O flag informa um momento; a variável guarda um estado — gravada como verdadeira quando o flag é acionado, como falsa quando a contagem regressiva termina, e emitida a cada candle, de modo que ambos os filtros sempre têm um valor atual para combinar com os outros dois sinais.
- O painel do gráfico desenha os candles, o Rate of Change, o resultado realizado, as ordens de entrada e saída e cada execução, portanto um trecho em que os sinais foram atingidos mas nenhuma ordem se seguiu é fácil de reconhecer como uma pausa.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
