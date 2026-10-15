# Diagrama de estratégia de cruzamento de EMA com stop móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A entrada deste diagrama é a coisa mais simples da paleta: um preço de fechamento cruzando uma média exponencial em candles de quatro horas. O que o exemplo realmente aborda é o bloco que retira a posição depois. O Position protection é armado pela execução de entrada, recebe um preço a cada candle fechado e, com o trailing ligado, arrasta o stop atrás de uma operação que corre a favor, de modo que a saída passa a ser um nível que avança em catraca em vez de uma linha fixa na entrada.

![schema](schema.svg)

## Visão geral da estratégia

- Os candles de quatro horas são o relógio do diagrama, e apenas os já concluídos são publicados, de modo que toda decisão é tomada sobre uma barra que já está completa.
- Um conversor lê o preço de fechamento de cada candle, e uma média exponencial construída sobre a mesma série é o nível contra o qual o preço é medido.
- Dois blocos de cruzamento observam esse par por lados opostos: um toma o fechamento como entrada superior e a média como entrada inferior, o outro os tem na ordem contrária. Cada um se manifesta apenas na barra em que as duas linhas realmente trocam de lugar.
- A posição atual é encaixada no compasso do candle por uma variável que a retém em seu conector de entrada e a libera no gatilho do candle; três comparações contra zero transformam esse número retido em zerado, comprado e vendido.
- Quatro portas lógicas AND combinam os dois cruzamentos com esses três estados: um cruzamento para cima estando zerado abre uma posição comprada, um cruzamento para baixo estando zerado abre uma posição vendida, e qualquer um dos cruzamentos contra uma posição existente a zera.
- Ambos os blocos de entrada carregam a condição de posição aberta, de modo que uma porta que dispara enquanto uma operação já está em curso não pode nem aumentá-la nem invertê-la: o diagrama mantém uma posição por vez.
- Cada execução própria chega ao Position protection através de um Combination, que é o que mantém honesta a sua noção da posição — as entradas o armam, as execuções de zeragem o desarmam.
- Seu conector Price recebe o mesmo preço de fechamento que os sinais usam, de modo que o nível do stop móvel é recalculado uma vez por candle fechado; o painel de gráfico desenha os candles, a média, as ordens de entrada e saída, o stop de proteção e cada execução.

## Regras de entrada e saída

- **Entrada comprada**: Em um candle fechado, o preço de fechamento cruza acima da média exponencial enquanto a posição retida é zero. A porta de compra libera o sinal e o Position modify compra a Order Volume a mercado.
- **Entrada vendida**: Em um candle fechado, o preço de fechamento cruza abaixo da média exponencial enquanto a posição retida é zero. A porta de venda libera o sinal e o Position modify vende a Order Volume a mercado.
- **Saída**: Duas coisas podem encerrar uma operação, e normalmente é a primeira que o faz. O Position protection coloca seu stop a 1.5% do preço de entrada e, com o trailing ligado, o carrega para cima atrás de uma posição comprada e para baixo atrás de uma vendida sempre que um candle fecha mais adentro do lucro; a operação é encerrada por uma ordem a mercado assim que um fechamento volta a atravessar esse nível. A segunda saída é o próprio sinal: um cruzamento contra uma posição aberta aciona um terceiro bloco Position modify configurado para fechar, que zera o que houver e não precisa de quantidade própria. Nenhum dos caminhos inverte a posição — a direção oposta tem de esperar o cruzamento seguinte, quando o diagrama já está zerado e livre para tomá-la.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 04:00:00 | Time frame da série de candles. Um candle fechado é uma decisão e um recálculo do nível do stop móvel. |
| EMA Length | 15 | Número de candles na média exponencial contra a qual o preço de fechamento é medido. Uma média mais longa cruza com menos frequência e sustenta uma operação através de mais ruído. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |
| Trailing Stop, % | 1.5 | Distância do stop móvel, em percentual. É medida a partir do melhor preço alcançado desde a abertura da posição, e não a partir do preço de entrada. |

## Detalhes do diagrama

- O nível do stop móvel segue o preço que o bloco recebe, e aqui esse preço é um fechamento. O stop fica, portanto, onde os candles fecharam, e não onde chegaram suas sombras, de modo que um pico dentro da barra nem arrasta o nível nem derruba a operação.
- Um trailing mais fino está a uma ligação de distância: alimente o conector Price a partir de um bloco Level 1 lendo o preço do último negócio, ou entregue o livro de ofertas ao conector Market depth, e o mesmo stop passa a ser reprecificado a cada cotação, em vez de uma vez a cada quatro horas.
- Cada execução própria vai para o Position protection, inclusive as de zeragem. Uma execução de fechamento leva a posição corrente do bloco de volta a zero e desarma o stop; sem ela, o bloco continuaria vigiando uma posição que já não existe e acabaria por protegê-la abrindo a posição oposta.
- A média publica apenas valores formados e definitivos, de modo que as barras iniciais, em que ela ainda está se preenchendo, não podem produzir um cruzamento próprio.
- Order Volume é um único número exposto e compartilhado pelos dois blocos de entrada. O bloco de fechamento não toma quantidade alguma, porque uma ordem de fechamento de posição se dimensiona a partir do que estiver aberto.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
