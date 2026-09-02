# Diagrama da estratégia de tendência MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama segue a tendência com o MACD: a diferença entre uma média móvel exponencial rápida e uma lenta é suavizada mais uma vez para formar a linha de sinal, e cada cruzamento entre as duas linhas inverte a posição. O volume da ordem já inclui a posição aberta, de modo que uma única ordem fecha o que está aberto e abre o lado oposto.

![schema](schema.svg)

## Visão geral da estratégia

- O MACD é montado no diagrama a partir de suas peças: EMA(12) menos EMA(26) é a linha MACD, e uma EMA(9) dessa linha é a linha de sinal, o que mantém os três períodos como parâmetros do esquema.
- Um bloco de cruzamento compara as duas linhas e dispara apenas no candle em que elas realmente se cruzam, para cima ou para baixo.
- Depois do primeiro sinal a estratégia está sempre no mercado: não há saída própria, o cruzamento contrário inverte a posição.

## Regras de entrada e saída

- **Entrada comprada**: A linha MACD cruza acima da linha de sinal e a posição ainda não está comprada. A ordem compra Volume mais o valor absoluto da posição atual: abre uma compra a partir do zero ou vira uma venda diretamente em compra.
- **Entrada vendida**: A linha MACD cruza abaixo da linha de sinal e a posição ainda não está vendida. A ordem vende Volume mais o valor absoluto da posição atual: abre uma venda a partir do zero ou vira uma compra diretamente em venda.
- **Saída**: Não há bloco de saída próprio nem stop de proteção: só o cruzamento contrário tira da posição, invertendo-a em uma única ordem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA length | 12 | Período da média móvel exponencial rápida dentro do MACD. |
| Slow EMA length | 26 | Período da média móvel exponencial lenta dentro do MACD. |
| Signal EMA length | 9 | Período de suavização da linha de sinal construída sobre a linha MACD. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta as duas médias e um bloco de fórmula subtrai a lenta da rápida, produzindo a linha MACD.
- A linha MACD segue para um terceiro bloco de indicador, uma EMA(9), que é a linha de sinal; as duas linhas se encontram no bloco de cruzamento.
- A saída do cruzamento é o sinal de compra, um NÃO lógico dela é o sinal de venda, e cada um se junta por um E lógico à comparação da posição com zero.
- Um segundo bloco de fórmula calcula Volume mais a posição em valor absoluto e alimenta a entrada de volume dos dois blocos de modificação de posição: é assim que uma ordem a mercado inverte a posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
