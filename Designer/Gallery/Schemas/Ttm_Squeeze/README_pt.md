# Diagrama da estratégia TTM Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Mercados calmos não permanecem calmos. Este diagrama mede a largura das bandas de Bollinger como percentual da banda média, considera o mercado comprimido enquanto essa largura fica abaixo da sua própria média móvel e negocia no primeiro candle em que as bandas voltam a se abrir. O RSI define a direção.

![schema](schema.svg)

## Visão geral da estratégia

- Largura = (banda superior - banda inferior) / banda média * 100, de modo que a leitura da compressão não depende do nível de preço do ativo.
- Uma média móvel simples dessa largura, multiplicada pelo fator de compressão, marca a linha abaixo da qual o mercado é considerado comprimido.
- A operação acontece na expansão, não na compressão: o candle anterior precisava estar dentro da compressão e a largura atual precisa superá-la.
- O RSI em relação à linha média dá a direção, e a banda de Bollinger oposta é onde a operação é abandonada.

## Regras de entrada e saída

- **Entrada comprada**: A largura supera a do candle anterior, esse valor anterior estava no nível de compressão ou abaixo dele, o RSI está acima de 50 e a posição está zerada. A ordem de compra abre uma posição comprada de um lote.
- **Entrada vendida**: A largura supera a do candle anterior, esse valor anterior estava no nível de compressão ou abaixo dele, o RSI está abaixo de 50 e a posição está zerada. A ordem de venda abre uma posição vendida de um lote.
- **Saída**: A compra é encerrada quando o fechamento cai abaixo da banda inferior e a venda quando sobe acima da banda superior: o rompimento falhou e foi para o outro lado. As duas saídas usam o modo de encerramento; a estratégia original também não tem stop nem alvo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Period | 20 | Período de suavização das bandas de Bollinger. |
| Bollinger Width | 2 | Largura das bandas de Bollinger, em desvios padrão. |
| RSI Length | 14 | Período do RSI que confirma a direção. |
| Width Average Length | 20 | Comprimento da média móvel calculada sobre a própria largura das bandas. |
| Squeeze Factor | 0.9 | Fração dessa média abaixo da qual o mercado é considerado comprimido; reduza-a para sinais mais raros e exigentes. |
| RSI Midline | 50 | Nível do RSI que separa a leitura de alta da de baixa. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:30:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de Bollinger é lido por três conversores: banda superior, banda inferior e banda média; um quarto conversor pega o fechamento do candle.
- Um bloco de fórmula transforma as três bandas na largura percentual, que alimenta tanto um bloco de média móvel quanto um bloco de valor anterior, permitindo comparar a largura com o seu próprio passado.
- Uma segunda fórmula multiplica a largura média pelo fator de compressão, e duas comparações produzem os sinais de compressão e de expansão.
- Cada entrada é um E lógico de quatro condições: expansão, compressão, direção do RSI e posição zerada; os dois blocos de entrada tiram o volume da mesma constante.
- A estratégia original ainda mantém um mínimo corrente da largura, conta três barras estreitas, filtra a direção com uma EMA(20) e faz pausa de quinze barras após cada operação; o diagrama troca esse mínimo pela média móvel da largura e abre mão do contador, da EMA e da pausa, que nenhum bloco consegue expressar.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
