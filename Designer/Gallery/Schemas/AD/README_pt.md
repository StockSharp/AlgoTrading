# Diagrama da estratégia de tendência da linha de acumulação/distribuição
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Aqui é o volume que define a direção. A linha de acumulação/distribuição soma onde cada candle fechou dentro da sua própria faixa, ponderado pelo volume negociado: uma linha em alta significa que os compradores absorveram a oferta e uma linha em queda, o contrário. O diagrama compara a linha com o seu valor de um candle antes e fica do lado que o volume sustenta, desde que a média móvel simples concorde.

![schema](schema.svg)

## Visão geral da estratégia

- A linha de acumulação/distribuição recebe o candle inteiro, pois precisa de máxima, mínima, fechamento e volume ao mesmo tempo.
- Um bloco de valor anterior guarda a leitura de um candle atrás, de modo que a inclinação da linha vira uma comparação simples e não um segundo indicador.
- A média móvel simples funciona como filtro de permissão: pode haver fluxo de volume, mas só há compra se o candle também fechar acima da média.
- As duas entradas trazem a condição de abrir posição e as duas saídas a de fechar, então apenas uma posição é mantida e nunca aumentada.

## Regras de entrada e saída

- **Entrada comprada**: A linha A/D está acima do seu valor anterior, o candle fecha acima da média móvel simples e a posição está zerada. A ordem compra o volume compartilhado a mercado.
- **Entrada vendida**: A linha A/D está no seu valor anterior ou abaixo, o candle fecha abaixo da média móvel simples e a posição está zerada. A ordem vende o volume compartilhado a mercado.
- **Saída**: Só a inclinação encerra a operação, sem condição de preço: a linha recuando fecha uma compra e a linha virando para cima fecha uma venda. Não há stop loss nem take profit, exatamente como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MA Period | 20 | Período da média móvel simples que decide para que lado a entrada é permitida. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta três consumidores ao mesmo tempo: a linha A/D, a média móvel e o conversor que extrai o preço de fechamento.
- A saída da linha A/D vai tanto para o bloco de valor anterior quanto direto para duas comparações, então alta e queda são lidas do mesmo par de números.
- Cada E lógico une a inclinação da linha, o lado da média móvel e a checagem de posição zerada antes de acionar um bloco de entrada.
- Os dois blocos de saída ficam ligados diretamente às comparações de inclinação e trazem a condição de fechar posição, o que faz cada um agir em um único sentido.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
