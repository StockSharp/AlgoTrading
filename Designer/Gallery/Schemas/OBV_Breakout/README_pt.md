# Diagrama da estratégia de direção do OBV com filtro de média móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O On-Balance Volume soma o volume de cada candle de alta e subtrai o de cada candle de baixa, de modo que a sua inclinação mostra qual lado está negociando. Este diagrama lê apenas essa inclinação, candle a candle, e deixa uma média móvel simples do preço decidir quando vale a pena segui-la. Apesar do nome da pasta, o sinal compara o OBV somente com seu valor no candle anterior, não com um nível de rompimento.

![schema](schema.svg)

## Visão geral da estratégia

- O On-Balance Volume é calculado sobre candles finalizados e comparado com o seu valor um candle atrás, o que dá um veredito simples: sobe ou não sobe.
- Uma média móvel simples de vinte candles sobre o preço de fechamento divide o gráfico em metade superior e inferior e define a direção da entrada.
- A entrada só acontece a partir da posição zerada, de modo que os dois lados nunca brigam dentro da mesma operação.
- A saída não precisa da média: a posição é abandonada assim que o fluxo de volume vira contra ela.

## Regras de entrada e saída

- **Entrada comprada**: O On-Balance Volume está acima do seu valor no candle anterior, o candle fechou acima da média móvel e a posição está zerada. A ordem compra um lote a mercado.
- **Entrada vendida**: O On-Balance Volume está no valor anterior ou abaixo dele, o candle fechou abaixo da média móvel e a posição está zerada. A ordem vende um lote a mercado. Um OBV inalterado conta como não ascendente.
- **Saída**: Uma compra é encerrada no primeiro candle em que o OBV deixa de subir e uma venda no primeiro candle em que ele volta a subir, ambos por blocos de modificação de posição em modo de fechamento. Não há stop loss nem take profit.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período da média móvel simples que define a direção da entrada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de On-Balance Volume, o da média móvel e o conversor que lê o preço de fechamento; um bloco de valor anterior com deslocamento de um candle entrega o OBV passado, e dois blocos de comparação transformam o par em uma flag de subida e outra de não subida.
- Cada E lógico une a flag do OBV, a posição do preço em relação à média e a checagem de posição zerada, e aciona um bloco de modificação de posição no modo somente abertura.
- As mesmas duas flags do OBV vão direto para os blocos de fechamento, que estão em modo de fechamento e por isso ficam parados enquanto o diagrama está zerado.
- O diagrama trabalha com candles de cinco minutos do histórico empacotado e negocia todo sinal válido.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
