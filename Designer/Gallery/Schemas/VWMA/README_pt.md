# Diagrama da estratégia de cruzamento do preço com a VWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A média móvel ponderada por volume pesa cada preço pelo volume negociado nele, por isso se aproxima dos níveis onde o dinheiro realmente trocou de mãos. O diagrama acompanha a passagem do preço de fechamento por essa média: se o fechamento passa de baixo para cima, compra; no sentido contrário, vende. A estratégia original usa candles de um minuto e descansa algumas barras após cada negócio; o diagrama trabalha em cinco minutos e deixa a pausa de fora, já que a verificação da posição impede uma segunda entrada no mesmo sentido.

![schema](schema.svg)

## Visão geral da estratégia

- O VolumeWeightedMovingAverage recebe o candle inteiro e não apenas um preço, porque também precisa do volume negociado.
- Tanto o fechamento quanto a média são guardados um candle atrás, de modo que o cruzamento é lido exatamente como no código original.
- Toda entrada é protegida pela posição: só se compra enquanto a posição não estiver comprada e só se vende enquanto não estiver vendida.
- A pausa da estratégia original não foi reproduzida, então o diagrama responde a cada cruzamento que enxerga.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento anterior estava na VWMA anterior ou abaixo dela e o fechamento atual está acima da VWMA atual, enquanto a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda a encerra.
- **Entrada vendida**: O fechamento anterior estava na VWMA anterior ou acima dela e o fechamento atual está abaixo da VWMA atual, enquanto a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra a encerra.
- **Saída**: Não há bloco de saída próprio nem stop de proteção: o cruzamento contrário zera a posição, pois todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| VWMA Length | 14 | Período de suavização da média móvel ponderada por volume. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles de todo o diagrama; o original usava um minuto. |

## Detalhes do diagrama

- O bloco de candles alimenta dois ramos ao mesmo tempo: o bloco de indicador com VolumeWeightedMovingAverage e um conversor que extrai o preço de fechamento.
- Dois blocos de valor anterior guardam o fechamento e a média do candle precedente.
- Quatro blocos de comparação montam os dois cruzamentos, outros dois comparam a posição com uma constante zero e cada E lógico reúne três desses sinais.
- Ambos os blocos de modificação de posição enviam ordens a mercado com o volume de uma única constante compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
