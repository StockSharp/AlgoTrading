# Diagrama da estratégia de rompimento do canal de Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A ideia de seguimento de tendência mais antiga que existe: o indicador Donchian Channels desenha a máxima mais alta e a mínima mais baixa dos últimos N candles, e a estratégia entra no movimento assim que um candle fecha fora desse canal. Ela está sempre no mercado e vira de comprada para vendida, e vice-versa, no rompimento contrário.

![schema](schema.svg)

## Visão geral da estratégia

- Os Donchian Channels são calculados sobre candles finalizados: a banda superior é a máxima do período e a inferior, a mínima.
- As duas bandas são atrasadas em um candle, de modo que o fechamento atual é comparado com um canal já encerrado; caso contrário, o próprio candle elevaria a banda que deveria romper.
- A posição atual participa de cada decisão e ao volume da ordem soma-se o módulo da posição, assim uma única ordem a mercado encerra o lado antigo e abre o novo.

## Regras de entrada e saída

- **Entrada comprada**: O candle fecha acima da banda superior do candle anterior e a posição não está comprada. A ordem compra o volume base mais o módulo da posição: vira uma venda em compra ou abre uma compra a partir do zero.
- **Entrada vendida**: O candle fecha abaixo da banda inferior do candle anterior e a posição não está vendida. A ordem vende o volume base mais o módulo da posição: vira uma compra em venda ou abre uma venda a partir do zero.
- **Saída**: Não há stop, nem alvo, nem bloco de saída próprio: a posição é mantida até que o rompimento contrário a inverta, exatamente como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Channel period | 20 | Número de candles sobre os quais são tomadas a máxima e a mínima. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se o módulo da posição. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o indicador Donchian Channels e, por um conversor, o preço de fechamento.
- Dois conversores extraem do indicador os valores UpperBand e LowerBand, e dois blocos de valor anterior os deslocam um candle para trás.
- Dois blocos de comparação testam o fechamento contra as bandas deslocadas; outros dois comparam a posição com zero, e um E lógico junta uma condição de cada tipo no sinal de entrada.
- Um bloco de fórmula calcula o volume de inversão como volume base mais o módulo da posição e o envia aos dois blocos de modificação de posição.
- O código original usa por padrão um canal de 1000 candles de um minuto; o diagrama adota um canal de 20 candles de cinco minutos, o valor descrito no README da estratégia e na sua faixa de otimização, para que realmente opere em um mês de histórico.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
