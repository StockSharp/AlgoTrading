# Diagrama da estratégia de candles Heikin-Ashi consecutivos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os candles Heikin-Ashi eliminam o ruído por média, então a cor deles permanece igual enquanto o movimento realmente dura. Este diagrama mede essa persistência: sete corpos de alta seguidos são tratados como tendência estabelecida e comprados, sete corpos de baixa seguidos são vendidos, e um stop loss percentual limita o custo de uma sequência falsa.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de fórmula monta o corpo Heikin-Ashi como a média de abertura, máxima, mínima e fechamento menos o ponto médio do candle anterior: corpo positivo é candle de alta, negativo é de baixa.
- A sequência de candles da mesma cor é medida sem contador: a mínima dos últimos sete corpos acima de zero significa que os sete foram de alta, e a máxima abaixo de zero, que os sete foram de baixa.
- A ordem é dimensionada como volume mais a posição absoluta, de modo que uma única ordem vira uma venda direto para compra e vice-versa, exatamente como no original em C#.
- A abertura Heikin-Ashi é definida pelo seu próprio valor anterior, algo que um diagrama não consegue realimentar em um bloco; em seu lugar usa-se o ponto médio do candle comum anterior, então as sequências encontradas aqui ficam próximas, mas não idênticas, às contadas pelo código fonte.

## Regras de entrada e saída

- **Entrada comprada**: A mínima dos últimos sete corpos Heikin-Ashi está acima de zero, ou seja, os sete candles foram de alta, e a posição não está comprada. A ordem compra volume mais a posição absoluta: abre uma compra do zero ou vira uma venda.
- **Entrada vendida**: A máxima dos últimos sete corpos Heikin-Ashi está abaixo de zero, ou seja, os sete candles foram de baixa, e a posição não está vendida. A ordem vende volume mais a posição absoluta: abre uma venda do zero ou vira uma compra.
- **Saída**: Não há regra de saída própria, como na estratégia de origem: a posição é virada pela sequência contrária ou retirada pelo bloco de proteção, que coloca um stop loss a uma porcentagem fixa do preço de execução. Não há alvo nem stop móvel.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Consecutive candles | 7 | Quantos candles Heikin-Ashi da mesma cor em sequência formam um sinal; é o período tanto do bloco Lowest quanto do Highest. |
| Stop loss, % | 2 | Distância do stop loss em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume base da ordem, em lotes; a posição absoluta é somada para que a virada aconteça em uma única ordem. |
| Candles | 00:30:00 | Tempo gráfico dos candles de todo o diagrama, a mesma meia hora usada pela estratégia original. |

## Detalhes do diagrama

- O bloco de candles alimenta quatro conversores de abertura, máxima, mínima e fechamento, e dois blocos de valor anterior entregam o candle anterior à fórmula.
- A saída da fórmula entra em um bloco Lowest e um Highest de mesmo período, e duas comparações com a constante zero os transformam nas duas condições de sequência.
- O bloco de posição, comparado duas vezes com zero, entra por um E lógico em cada condição, de modo que nenhuma ordem aumenta uma posição já orientada corretamente.
- Os dois blocos de modificação tiram o tamanho de uma fórmula que soma a posição absoluta ao volume compartilhado, e suas execuções alimentam o bloco de proteção com o stop loss.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
