# Diagrama da estratégia de entrada no repique da EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama de tendência que se recusa a comprar o rompimento. As duas médias móveis exponenciais decidem a direção e a entrada espera o fechamento voltar a tocar a média rápida, de modo que a posição é aberta a um preço melhor dentro de um movimento já em curso. A saída é decidida pela própria tendência: a posição é encerrada assim que as médias trocam de lugar.

![schema](schema.svg)

## Visão geral da estratégia

- Duas médias móveis exponenciais do fechamento, uma rápida de 8 e uma lenta de 21, definem para que lado o diagrama pode operar.
- Um bloco de cruzamento acompanha o fechamento em relação à média rápida, então o repique é capturado exatamente no candle em que o preço volta à média, e não em todo candle próximo a ela.
- Entradas e saídas seguem por ramos separados: dois blocos de modificação de posição abrem com o volume da ordem e outros dois apenas encerram o que já está aberto.

## Regras de entrada e saída

- **Entrada comprada**: A EMA rápida está acima da lenta, o fechamento volta para baixo até a EMA rápida e a posição não está comprada. A ordem compra Volume mais o valor absoluto da posição atual: abre uma compra a partir do zero ou vira uma venda diretamente em compra.
- **Entrada vendida**: A EMA rápida está abaixo da lenta, o fechamento volta para cima até a EMA rápida e a posição não está vendida. A ordem vende Volume mais o valor absoluto da posição atual: abre uma venda a partir do zero ou vira uma compra diretamente em venda.
- **Saída**: A compra é encerrada quando a EMA rápida cai abaixo da lenta, e a venda quando a rápida sobe acima dela; ambos os blocos de encerramento atuam sobre toda a posição aberta, então um sinal repetido sem posição não faz nada. Não há stop de proteção, exatamente como a estratégia original foi escrita.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA length | 8 | Período da média móvel exponencial rápida, aquela até a qual o preço retorna. |
| Slow EMA length | 21 | Período da média móvel exponencial lenta, que define a direção da tendência. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta as duas médias e um conversor que lê o preço de fechamento.
- O bloco de cruzamento recebe a EMA rápida na entrada superior e o fechamento na inferior, então sua saída verdadeira é o fechamento voltando para baixo até a média, e um NÃO lógico dela é o retorno para cima.
- Dois blocos de comparação confrontam as médias entre si e outros quatro comparam a posição com uma constante zero compartilhada, produzindo tanto os filtros de entrada quanto os de saída.
- O ramo de entrada obtém o volume de uma fórmula que soma a posição absoluta à constante de volume, enquanto os dois blocos de encerramento estão configurados para fechar a posição e não precisam de volume algum.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
