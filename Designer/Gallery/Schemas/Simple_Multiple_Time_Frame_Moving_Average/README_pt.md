# Diagrama da estratégia Simple Multiple Time Frame Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O nome promete dois tempos gráficos, mas a estratégia em C# de onde ele vem assina uma única série de quatro horas e calcula sobre ela duas ExponentialMovingAverage de comprimentos diferentes. O que realmente se negocia é a concordância das inclinações: enquanto a curta e a longa apontam para cima o diagrama fica comprado, enquanto ambas apontam para baixo fica vendido, e havendo divergência a posição é deixada em paz.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos ExponentialMovingAverage, um curto e um longo, trabalham sobre a mesma série de candles; o diagrama mantém essa assinatura única em vez de inventar um segundo tempo gráfico.
- A inclinação de cada média é lida comparando o valor atual com um bloco de valor anterior de um candle: uma média que sobe é simplesmente uma média acima de onde estava.
- Todas as ordens usam o volume compartilhado fixo, então o sinal contrário apenas zera a posição; abrir para o outro lado exige um segundo sinal do mesmo sentido no candle seguinte, exatamente como no código de origem.
- A condição é um estado, não um evento: ela é reavaliada a cada candle encerrado, por isso bastam comparações e E lógicos e nenhum bloco de cruzamento é necessário.

## Regras de entrada e saída

- **Entrada comprada**: A ExponentialMovingAverage rápida está acima do próprio valor um candle atrás, a lenta também, e a posição não está comprada. O bloco de modificação compra a mercado o volume compartilhado: abre uma compra do zero ou encerra uma venda existente.
- **Entrada vendida**: A ExponentialMovingAverage rápida está abaixo do próprio valor um candle atrás, a lenta também, e a posição não está vendida. O bloco de modificação vende a mercado o volume compartilhado: abre uma venda do zero ou encerra uma compra existente.
- **Saída**: Não há regra de saída própria: a posição é encerrada pelo sinal contrário, isto é, quando as duas médias viram para o outro lado. A estratégia de origem não tem stop loss, alvo nem pausa entre operações, e este diagrama também não.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA length | 5 | Período da ExponentialMovingAverage rápida. |
| Slow EMA length | 20 | Período da ExponentialMovingAverage lenta. |
| Volume | 1 | Volume da ordem, em lotes; a mesma constante alimenta os dois blocos de modificação. |
| Candles | 04:00:00 | Tempo gráfico dos candles de todo o diagrama; o original usa quatro horas e isso foi mantido, o que dá cerca de duzentos candles no mês de histórico incluído. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois blocos de indicador, e cada indicador alimenta um bloco de valor anterior tipado como valor de indicador.
- Quatro blocos de comparação transformam as duas médias e suas cópias atrasadas em sinalizadores de alta e de baixa.
- O bloco de posição, comparado duas vezes com a constante zero, fornece a verificação que impede uma entrada de aumentar uma posição já aberta.
- Cada E lógico une uma condição da média rápida, uma da lenta e uma da posição, e aciona um bloco de modificação que tira o tamanho da constante de volume compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
