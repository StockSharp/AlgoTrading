# Diagrama da estratégia MA + Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma média móvel simples diz de que lado do mercado vale a pena estar e um Parabolic SAR diz quando: o diagrama espera o fechamento cruzar a linha do SAR na direção para a qual a média já aponta. O cruzamento contrário da mesma linha devolve a posição, então a estratégia ou está montada numa tendência ou aguarda a próxima.

![schema](schema.svg)

## Visão geral da estratégia

- SimpleMovingAverage é o filtro de direção: compra-se apenas enquanto o fechamento está acima dela e vende-se apenas enquanto está abaixo.
- ParabolicSar fornece o momento, e um único bloco de cruzamento transforma a passagem do preço por essa linha num pulso só: verdadeiro para o cruzamento de alta, falso para o de baixa.
- As entradas são protegidas pela posição atual e as saídas usam blocos de encerramento, que agem apenas quando existe posição do sinal correspondente.
- Dois desvios do original em C#: lá o SAR é substituído por uma EMA rápida e os ajustes declarados do SAR nunca são lidos, enquanto o diagrama usa um ParabolicSar de verdade; além disso, a pausa de 20 barras entre entradas não é reproduzida.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento cruza a linha do ParabolicSar para cima estando acima da SMA e a posição não está comprada. O bloco de modificação compra a mercado o volume compartilhado.
- **Entrada vendida**: O fechamento cruza a linha do ParabolicSar para baixo estando abaixo da SMA e a posição não está vendida. O bloco de modificação vende a mercado o volume compartilhado.
- **Saída**: A compra é encerrada no primeiro cruzamento de baixa da linha do SAR e a venda no primeiro cruzamento de alta, sem consultar a média móvel; não há stop nem alvo, como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período da média móvel simples que define a direção da tendência. |
| SAR Acceleration | 0.02 | Fator de aceleração inicial do Parabolic SAR. |
| SAR Max acceleration | 0.2 | Teto até o qual cresce o fator de aceleração do Parabolic SAR. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois indicadores e um conversor que lê o preço de fechamento.
- O bloco de cruzamento compara o fechamento com a linha do SAR, e um NÃO lógico transforma sua saída no cruzamento de baixa usado pela entrada vendida e pela saída comprada.
- Blocos de comparação testam o fechamento contra a SMA e a posição contra uma constante zero, e quatro E lógicos formam os sinais de entrada e saída.
- Dois blocos de modificação abrem posições com a constante de volume compartilhada e outros dois as encerram com a condição de fechar posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
