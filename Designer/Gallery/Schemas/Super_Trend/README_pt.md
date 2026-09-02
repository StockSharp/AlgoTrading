# Diagrama da estratégia de virada com Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Supertrend desenha uma única linha que fica abaixo do preço em tendência de alta e acima dele em tendência de baixa, a uma distância de vários intervalos verdadeiros médios em relação ao preço mediano. O diagrama opera no momento em que o fechamento atravessa essa linha: compra o passo para cima, vende o passo para baixo e mantém o lado até a próxima virada.

![schema](schema.svg)

## Visão geral da estratégia

- O indicador Supertrend é calculado sobre candles finalizados: o período do ATR define a que distância a linha fica do preço e o multiplicador escala essa distância.
- Um conversor extrai o preço de fechamento de cada candle e um bloco de cruzamento o compara com a linha Supertrend, disparando apenas no candle em que de fato se cruzam.
- Depois do primeiro sinal a estratégia está sempre no mercado: não há stop nem alvo, apenas a virada da linha.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento cruza acima da linha Supertrend e a posição ainda não está comprada. A ordem compra Volume mais o valor absoluto da posição atual: abre uma compra a partir do zero ou vira uma venda diretamente em compra.
- **Entrada vendida**: O fechamento cruza abaixo da linha Supertrend e a posição ainda não está vendida. A ordem vende Volume mais o valor absoluto da posição atual: abre uma venda a partir do zero ou vira uma compra diretamente em venda.
- **Saída**: Não há saída própria nem stop de proteção: só a virada contrária da linha tira da posição, invertendo-a em uma única ordem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| ATR period | 10 | Período do ATR sobre o qual a linha Supertrend é construída. |
| ATR multiplier | 3 | Multiplicador aplicado ao ATR, que define o afastamento da linha em relação ao preço mediano. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador com o Supertrend e, por meio de um conversor, fornece o preço de fechamento do mesmo candle.
- Ambos entram no bloco de cruzamento, cuja saída é o sinal de compra, enquanto um NÃO lógico dela é o sinal de venda.
- Cada sinal se junta por um E lógico à comparação da posição com zero, de modo que uma entrada nunca aumenta uma posição já aberta naquele lado.
- Um bloco de fórmula calcula Volume mais a posição em valor absoluto e alimenta a entrada de volume dos dois blocos de modificação de posição: é assim que uma ordem a mercado inverte a posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
