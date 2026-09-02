# Diagrama da estratégia de stop móvel por ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

As entradas são a parte simples: com a posição zerada, um fechamento acima da média móvel compra e um fechamento abaixo vende. O interessante é a saída, um stop móvel por ATR: uma linha mantida a alguns intervalos verdadeiros médios de distância do preço, que acompanha o movimento favorável e nunca recua, encerrando a posição assim que o fechamento a rompe.

![schema](schema.svg)

## Visão geral da estratégia

- Uma média móvel simples de vinte períodos divide o gráfico em um lado de alta e outro de baixa, e a posição do fechamento em relação a ela decide a direção da entrada.
- O stop móvel é um bloco SuperTrend: trata-se exatamente de uma banda de ATR com catraca, de modo que a distância do stop respira com a volatilidade em vez de ser um número fixo de pontos.
- Toda entrada parte apenas de posição zerada e toda saída apenas de uma posição do lado correspondente, e é isso que impede os quatro blocos de ordem de atrapalharem uns aos outros.
- O nível do stop é largo de propósito — três vezes um ATR de catorze períodos — para que a posição resista ao ruído normal e só seja abandonada quando o movimento realmente vira.

## Regras de entrada e saída

- **Entrada comprada**: A posição está zerada e o candle fecha acima da média móvel simples. A ordem compra o volume compartilhado a mercado, e a linha de ATR abaixo do preço passa a ser o stop dessa compra.
- **Entrada vendida**: A posição está zerada e o candle fecha abaixo da média móvel simples. A ordem vende o volume compartilhado a mercado, e a linha de ATR acima do preço passa a ser o stop dessa venda.
- **Saída**: A compra é encerrada quando o fechamento cai abaixo da linha de ATR e a venda quando sobe acima dela. Não há take-profit nem inversão: depois do stop o diagrama espera zerado pelo próximo sinal da média móvel.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MA Period | 20 | Período da média móvel simples que decide a direção da entrada. |
| ATR Period | 14 | Período do ATR dentro da linha móvel; valores maiores fazem o stop reagir mais devagar a mudanças de volatilidade. |
| ATR Multiplier | 3 | Quantos ATR separam a linha do preço; valores maiores dão mais espaço à posição e geram menos saídas. |
| Volume | 1 | Volume da ordem, em lotes, compartilhado pelos quatro blocos de ordem. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta a média móvel, a linha SuperTrend e um conversor que lê o preço de fechamento.
- Duas comparações colocam o fechamento diante da média móvel e outras duas diante da linha móvel, de modo que o mesmo preço é lido uma vez e usado pelas duas metades do diagrama.
- Três comparações contra uma constante zero transformam a posição em sinalizadores de zerado, comprado e vendido, que liberam entradas e saídas separadamente.
- Os dois blocos de entrada carregam a condição de abertura e os dois de saída a de encerramento, então um sinal que não combina com a posição atual simplesmente não faz nada.
- A estratégia original recalcula o nível do stop como o máximo corrente do fechamento menos alguns ATR; essa catraca não se expressa como uma cadeia de blocos, por isso a linha SuperTrend, que funciona do mesmo modo, ocupa seu lugar.
- Vale conhecer mais duas simplificações: a pausa de quinhentos candles que o original mantém após cada operação não tem bloco equivalente e foi removida, e o diagrama roda em candles de cinco minutos em vez do minuto do código C#, porque esse é o histórico que acompanha a galeria.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
