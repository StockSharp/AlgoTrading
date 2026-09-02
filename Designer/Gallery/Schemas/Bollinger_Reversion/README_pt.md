# Diagrama da estratégia de reversão de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um fechamento fora de uma banda de Bollinger é tratado como um esticamento prestes a ser devolvido: o diagrama compra abaixo da banda inferior, vende acima da superior e mantém a posição apenas até o preço tocar novamente a linha média. Diferente de um diagrama de rompimento sobre as mesmas bandas, aqui a entrada é contra o movimento e o alvo é a linha média, não a banda oposta.

![schema](schema.svg)

## Visão geral da estratégia

- O BollingerBands é calculado uma vez e lido três vezes: banda superior, banda inferior e a média móvel central.
- A entrada ocorre apenas com posição zerada, de modo que uma sequência de fechamentos fora da banda não acrescenta nada a uma posição já aberta.
- A saída é simétrica à entrada: a linha média é o alvo e o bloco de encerramento envia exatamente o tamanho da posição aberta.
- A largura das bandas e seu período estão expostos, então o mesmo diagrama serve para um ativo calmo e para um volátil.

## Regras de entrada e saída

- **Entrada comprada**: O candle fecha abaixo da banda inferior e a posição está zerada. A ordem compra o volume base e abre uma compra contra o movimento.
- **Entrada vendida**: O candle fecha acima da banda superior e a posição está zerada. A ordem vende o volume base e abre uma venda contra o movimento.
- **Saída**: A compra é encerrada no primeiro fechamento na linha média ou acima dela; a venda, no primeiro fechamento na linha média ou abaixo. A estratégia original não tem stop nem take; sua pausa de quinhentos candles e seu limite de trezentos candles por posição não foram transpostos e, como a pausa era maior que o limite, no código-fonte toda operação terminava de fato por tempo e a saída na linha média nunca chegava a rodar.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Period | 20 | Período de suavização das bandas de Bollinger. |
| Bollinger Width | 2 | Largura das bandas em desvios padrão. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles: a estratégia original usava candles de um minuto e o diagrama trabalha com candles de cinco minutos. |

## Detalhes do diagrama

- O bloco de candles alimenta o indicador e um conversor do preço de fechamento; outros três conversores extraem as bandas e a linha média do valor do indicador.
- Quatro blocos de comparação transformam o fechamento em sinais: fora da banda inferior, fora da superior, de volta à média por baixo e de volta à média por cima.
- O bloco de posição alimenta três comparações com zero, que protegem as duas entradas e as duas saídas.
- Os blocos de entrada usam a condição de abertura e compartilham uma constante de volume; os de saída usam a condição de encerramento e tiram o volume da própria posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
