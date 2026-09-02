# Diagrama da estratégia Color Schaff Trend Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Schaff Trend Cycle é um estocástico calculado sobre o histograma do MACD, por isso reage mais rápido que um oscilador comum e ainda assim se move entre zero e cem. O diagrama negocia o instante em que o ciclo sai do meio dessa faixa e deixa uma simples linha MACD decidir se vale a pena seguir: só rompimentos para cima com MACD positivo e para baixo com MACD negativo viram ordens.

![schema](schema.svg)

## Visão geral da estratégia

- O Schaff Trend Cycle é calculado sobre candles finalizados, e um bloco de valor anterior guarda a leitura de um candle atrás para distinguir um rompimento do nível de simplesmente estar acima dele.
- Dois níveis delimitam o meio da faixa: cruzar o superior de baixo para cima é o gatilho de compra, cruzar o inferior de cima para baixo é o gatilho de venda.
- A linha MACD, diferença entre uma média móvel exponencial rápida e uma lenta, serve apenas de filtro de sinal: positiva libera compras, negativa libera vendas.
- Depois da primeira operação a estratégia está sempre no mercado: cada sinal inverte a posição, pois o volume da ordem é o volume base mais o que já está aberto.

## Regras de entrada e saída

- **Entrada comprada**: No candle anterior o ciclo estava no nível superior ou abaixo dele e agora está acima, a linha MACD é positiva e a posição não está comprada. A ordem compra o volume base mais o módulo da posição: vira uma venda em compra ou abre uma compra a partir do zero.
- **Entrada vendida**: No candle anterior o ciclo estava no nível inferior ou acima dele e agora está abaixo, a linha MACD é negativa e a posição não está vendida. A ordem vende o volume base mais o módulo da posição: vira uma compra em venda ou abre uma venda a partir do zero.
- **Saída**: Não há saída própria nem ordens de proteção, exatamente como na estratégia original: a posição só é abandonada quando chega o rompimento contrário do nível e a inverte.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| STC smoothing length | 10 | Período de suavização do Schaff Trend Cycle; valores maiores deixam o ciclo mais lento e os rompimentos mais raros. |
| MACD fast EMA | 12 | Média móvel exponencial rápida dentro do filtro MACD. |
| MACD slow EMA | 26 | Média móvel exponencial lenta dentro do filtro MACD. |
| Upper level | 60 | Nível que o ciclo precisa romper para cima para gerar sinal de compra. |
| Lower level | 40 | Nível que o ciclo precisa romper para baixo para gerar sinal de venda. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se o módulo da posição. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o Schaff Trend Cycle e o MACD; um bloco de valor anterior lê o ciclo um candle atrás.
- Quatro blocos de comparação montam os dois rompimentos: o valor anterior contra um nível e o valor atual contra o mesmo nível, o que em conjunto significa que a linha o atravessou neste candle.
- Outras duas comparações dão o sinal da linha MACD, e duas comparam a posição com a constante zero compartilhada, para que um sinal não aumente uma posição já aberta.
- Cada E lógico junta quatro condições - onde o ciclo estava, onde está, o sinal do MACD e a posição - e aciona um bloco de modificação de posição.
- Um bloco de fórmula calcula o tamanho da inversão como volume base mais o módulo da posição, de modo que uma ordem a mercado fecha o lado antigo e abre o novo, correspondendo ao par de ordens enviado pelo código C#.
- Vale conhecer duas diferenças em relação ao original em C#. O original leva o nome do Schaff Trend Cycle, mas na prática calcula um RSI de dez períodos no lugar dele; este diagrama usa o indicador Schaff Trend Cycle de verdade, então os sinais são os que o nome promete e não os que o código produz.
- Além disso, o original trabalha em candles de quatro horas, que deixam barras de menos no mês de histórico que acompanha a galeria; o diagrama roda em candles de cinco minutos.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
