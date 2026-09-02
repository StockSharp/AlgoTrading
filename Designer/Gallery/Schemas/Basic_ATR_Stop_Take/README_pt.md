# Diagrama da estratégia de stop e alvo por ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma lição curta sobre risco medido pela volatilidade. O fechamento que cruza a EMA de 50 abre a operação, o fechamento desse mesmo candle é guardado como preço de entrada e, a partir daí, o diagrama mede o quanto o preço se afastou dele em unidades do Average True Range. Um múltiplo do ATR encerra a operação no prejuízo e outro a encerra no lucro, de modo que a distância de saída cresce em mercados calmos e encolhe nos agitados, em vez de ser um número fixo de ticks.

![schema](schema.svg)

## Visão geral da estratégia

- Usa-se apenas um instrumento e uma série de candles: a EMA de 50 dá a direção e o ATR de 14 fornece a régua para as saídas.
- O preço de entrada é guardado por dois blocos de variável: o primeiro toma o fechamento do candle que gerou o sinal e o segundo o reemite a cada candle seguinte, para que as condições de saída sejam testadas continuamente.
- Dois blocos de fórmula convertem a distância até o preço de entrada em múltiplos de ATR, um a favor da compra e outro a favor da venda, de modo que os mesmos dois limiares servem aos dois lados.
- A saída é uma ordem a mercado em candle finalizado, exatamente como na estratégia de origem: não há stop pendurado na bolsa, então um pavio dentro do candle não tira a posição.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento cruza a EMA para cima com a posição zerada. Compra-se um lote e o fechamento desse candle passa a ser o preço de entrada.
- **Entrada vendida**: O fechamento cruza a EMA para baixo com a posição zerada. Vende-se um lote e o fechamento desse candle passa a ser o preço de entrada.
- **Saída**: A posição é encerrada no primeiro candle finalizado em que o preço andou StopFactor ATR contra o preço de entrada ou TakeFactor ATR a favor dele. Os dois blocos de modificação de posição estão no modo de encerramento, então cada um dispara apenas no seu lado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| EMA Length | 50 | Período da média móvel exponencial que o fechamento precisa cruzar. |
| ATR Length | 14 | Período do Average True Range que dimensiona o stop e o alvo. |
| Stop, ATR | 1.5 | Distância do stop, em ATR: o prejuízo que encerra a operação. |
| Take, ATR | 2 | Distância do alvo, em ATR: o lucro que encerra a operação. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:15:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta um conversor do preço de fechamento, a EMA e o ATR; um bloco de cruzamento compara o fechamento com a EMA e um NÃO lógico transforma o cruzamento para baixo no sinal de venda.
- A posição atual é comparada a uma constante zero e cada E lógico junta essa verificação a um cruzamento, de modo que só se abre operação a partir do zero.
- O preço de entrada é mantido por um par de blocos de variável; o segundo é acionado pela série de candles, e por isso essa é a última ligação que sai do bloco de candles — assim, já no candle de entrada a saída é medida contra o preço correto.
- Quatro blocos de comparação testam as duas distâncias em ATR contra as constantes de stop e alvo, dois blocos OU lógico as unem e dois blocos de modificação no modo de encerramento enviam as ordens de saída.
- A estratégia de origem espera seis candles entre operações. Um contador desses não tem equivalente entre os blocos, por isso o diagrama o omite e aproveita o cruzamento seguinte de imediato.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
