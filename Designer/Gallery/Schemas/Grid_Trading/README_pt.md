# Diagrama da estratégia de grade (grid trading)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama transforma o preço em uma escada: o fechamento de cada candle é arredondado para baixo até um múltiplo do passo da grade e apenas a mudança para um novo degrau conta como sinal. Um degrau acima compra, um degrau abaixo vende, de modo que a posição segue sempre o sentido em que a grade foi cruzada.

![schema](schema.svg)

## Visão geral da estratégia

- O preço de fechamento é discretizado pela fórmula floor(Close / GridStep) * GridStep, o que dá o degrau em que o mercado está.
- Um bloco de valor anterior guarda o degrau do candle passado, então são comparados degraus e não preços brutos, e qualquer movimento dentro de uma célula da grade é ignorado.
- O volume da ordem é a posição aberta mais o volume base, por isso um sinal contrário à posição a inverte com uma única ordem a mercado.
- A estratégia original opera em candles de quatro horas e fecha a posição com lucro absoluto de 2000 unidades de preço; aqui são usados candles de cinco minutos e o alvo é um percentual do preço de entrada, o que continua fazendo sentido em qualquer instrumento.

## Regras de entrada e saída

- **Entrada comprada**: O novo degrau da grade está acima do anterior e a posição não está comprada. A ordem compra o volume base mais a venda em aberto, deixando a posição comprada em um volume base.
- **Entrada vendida**: O novo degrau da grade está abaixo do anterior e a posição não está vendida. A ordem vende o volume base mais a compra em aberto, deixando a posição vendida em um volume base.
- **Saída**: O bloco de proteção de posição fecha a posição no take profit do percentual configurado; não há stop loss, como no original. Fora isso, a posição é mantida até o preço passar para a próxima célula da grade, onde o sinal contrário a inverte.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Grid Step | 500 | Altura de um degrau da grade, em unidades de preço do instrumento. |
| Take Profit, % | 3 | Take profit, em percentual do preço médio de entrada. |
| Volume | 1 | Volume base da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta um conversor que lê o preço de fechamento, e um bloco de fórmula arredonda esse preço para baixo até a grade.
- Um bloco de valor anterior atrasa o degrau em um candle; dois blocos de comparação decidem se o degrau subiu ou desceu.
- Duas comparações da posição com zero se juntam aos sinais da grade em blocos E lógico, de modo que a troca de degrau nunca aumenta uma posição já aberta naquele sentido.
- Uma segunda fórmula calcula |Position| + Volume e alimenta a entrada de volume dos dois blocos de modificação de posição — é por isso que a inversão sai em uma única ordem.
- As operações próprias dos dois blocos vão para o bloco de proteção de posição, cuja entrada de preço é o fechamento dos candles finalizados.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
