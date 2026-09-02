# Diagrama da estratégia de duplo cruzamento RSI + Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os dois osciladores precisam concordar no mesmo candle. O diagrama compra apenas quando o RSI cai abaixo de 30 e, ao mesmo tempo, o Williams %R cai abaixo de -80; vende apenas quando o RSI sobe acima de 70 e o Williams %R sobe acima de -20. Estar dentro da zona não basta: no candle anterior ambos precisavam continuar fora dela, e por isso cada oscilador também é guardado com um candle de atraso. A pausa de 180 barras do código original não foi reproduzida, porque em candles de cinco minutos ela silenciaria a estratégia por quinze horas depois de cada operação.

![schema](schema.svg)

## Visão geral da estratégia

- RSI 14 e Williams %R 14 são calculados sobre os mesmos candles de cinco minutos de um único instrumento.
- Blocos de valor anterior guardam os dois osciladores um candle atrás, o que separa uma entrada recente na zona de um valor que já está lá há horas.
- As entradas só acontecem com posição zerada, e é a linha média do RSI em 50 que devolve a posição ao zero.

## Regras de entrada e saída

- **Entrada comprada**: O RSI está abaixo do nível de sobrevenda e no candle anterior estava nele ou acima, e o Williams %R está abaixo do seu nível de sobrevenda e no candle anterior estava nele ou acima; a posição está zerada. Compra-se um lote a mercado.
- **Entrada vendida**: O RSI está acima do nível de sobrecompra e no candle anterior estava nele ou abaixo, e o Williams %R está acima do seu nível de sobrecompra e no candle anterior estava nele ou abaixo; a posição está zerada. Vende-se um lote a mercado.
- **Saída**: Uma compra é encerrada assim que o RSI volta acima da linha média de 50, e uma venda assim que o RSI cai abaixo dela; ambas as saídas são blocos de fechamento de posição, portanto cada uma mexe apenas no lado realmente aberto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| RSI Oversold | 30 | Nível que o RSI precisa romper para baixo para gerar sinal de compra. |
| RSI Overbought | 70 | Nível que o RSI precisa romper para cima para gerar sinal de venda. |
| Williams %R Length | 14 | Período de observação do Williams %R. |
| Williams %R Oversold | -80 | Nível que o Williams %R precisa romper para baixo na compra; o indicador vai de -100 a 0. |
| Williams %R Overbought | -20 | Nível que o Williams %R precisa romper para cima na venda. |
| RSI Midline | 50 | Nível neutro do RSI em que a posição aberta é entregue. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Cada oscilador alimenta um par de comparações, uma com o valor atual e outra com o anterior, de modo que o rompimento de um nível é descrito sem bloco de cruzamento, que permitiria que os dois rompimentos viessem de candles diferentes.
- Cada E lógico reúne cinco sinalizadores: as duas comparações do RSI, as duas do Williams %R e a posição zerada obtida ao comparar o bloco de posição com zero.
- Os dois blocos de entrada abrem posição apenas quando não há nenhuma e tiram o volume de uma constante compartilhada.
- Outras duas comparações acompanham o RSI em relação à sua linha média e acionam os blocos de fechamento, a única saída do diagrama.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
