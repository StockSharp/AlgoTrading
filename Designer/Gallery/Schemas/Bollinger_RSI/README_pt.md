# Diagrama da estratégia Bollinger Bands + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas ferramentas clássicas respondem aqui a perguntas diferentes. As Bollinger Bands mostram o quanto o preço se afastou da própria média, e o Relative Strength Index mostra se o movimento que causou esse afastamento já se esgotou. A operação só é aberta quando as duas concordam e é abandonada assim que o preço volta à banda central.

![schema](schema.svg)

## Visão geral da estratégia

- As Bollinger Bands e o Relative Strength Index são calculados sobre candles finalizados de um único instrumento.
- As bandas entregam três números ao diagrama de uma vez: banda superior, banda inferior e a média móvel central.
- Uma entrada exige fechamento fora de uma banda e um RSI na zona extrema correspondente; uma condição sozinha nunca basta.
- A banda central é o alvo: o retorno a ela encerra a posição, de modo que o diagrama não segura uma operação que já reverteu.

## Regras de entrada e saída

- **Entrada comprada**: O candle fecha abaixo da banda inferior de Bollinger, o RSI está abaixo do nível de sobrevenda e não há posição. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O candle fecha acima da banda superior de Bollinger, o RSI está acima do nível de sobrecompra e não há posição. A ordem vende um lote e abre uma venda.
- **Saída**: A compra é encerrada quando o fechamento volta acima da banda central, e a venda quando cai abaixo dela. Ambas as saídas usam blocos de modificação de posição em modo de fechamento, portanto só agem quando existe posição do lado correspondente; não há stop de proteção.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Length | 20 | Período de suavização das Bollinger Bands. |
| Bollinger Width | 2 | Multiplicador do desvio padrão que define a largura das bandas. |
| RSI Length | 14 | Período de suavização do Relative Strength Index. |
| RSI Oversold | 30 | Nível abaixo do qual o RSI é considerado sobrevendido. |
| RSI Overbought | 70 | Nível acima do qual o RSI é considerado sobrecomprado. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta três ramos: o bloco de Bollinger, o de RSI e um conversor que lê o preço de fechamento.
- Três blocos conversores separam o valor de Bollinger em banda superior, banda inferior e média móvel central.
- Seis blocos de comparação montam as condições: o fechamento contra cada banda, o RSI contra cada nível e a posição contra uma constante zero.
- Cada E lógico une uma condição de banda, uma de RSI e a verificação de posição, e aciona um bloco de modificação de posição cujo volume vem de uma constante compartilhada.
- A estratégia original faz uma pausa de um número fixo de barras após cada negócio; não existe bloco contador de barras, então a pausa foi omitida e apenas a banda central decide quando a operação termina.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
