# Diagrama da estratégia de reversão com VWMA e RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma média móvel ponderada por volume mostra onde o dinheiro realmente negociou, e o RSI diz se o afastamento dela foi exagerado. O diagrama compra abaixo da média apenas com o RSI sobrevendido, vende acima dela apenas com o RSI sobrecomprado e mantém a operação até o preço voltar para o outro lado da média.

![schema](schema.svg)

## Visão geral da estratégia

- A média é uma VolumeWeightedMovingAverage móvel de 32 candles, e não um VWAP de sessão. Apesar do nome, é o indicador que a estratégia original usa: ele pondera cada fechamento pelo volume do próprio candle.
- O índice de força relativa é calculado sobre preços de fechamento e apenas confirma a entrada; sozinho não abre nada.
- Os dois blocos de indicador emitem somente valores formados, o que impede operar com a média incompleta dos primeiros candles.
- O original para de processar candles por 100 barras depois de cada operação, o que congela também a saída e segura a posição por pelo menos oito horas. O Designer não tem contador de bloqueio, então essa pausa não foi reproduzida: aqui a posição é fechada assim que o preço cruza de volta a média.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está abaixo da VWMA, o RSI está abaixo do nível de sobrevenda e a posição está zerada. A ordem compra o volume configurado.
- **Entrada vendida**: O fechamento está acima da VWMA, o RSI está acima do nível de sobrecompra e a posição está zerada. A ordem vende o volume configurado.
- **Saída**: A compra é encerrada quando o fechamento volta acima da VWMA; a venda, quando volta abaixo dela. Não há stop loss nem take profit, como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| VWMA Length | 32 | Número de candles da média móvel ponderada por volume. |
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| Oversold | 30 | Nível abaixo do qual o índice é considerado sobrevendido. |
| Overbought | 70 | Nível acima do qual o índice é considerado sobrecomprado. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta diretamente a média ponderada por volume, que precisa do volume do candle, e alimenta o RSI por um conversor do preço de fechamento.
- Dois blocos de comparação colocam o fechamento de um lado ou de outro da média, e esses mesmos dois sinais servem tanto às entradas quanto às saídas.
- Outras duas comparações testam o RSI contra as constantes de limiar.
- O bloco de posição é comparado com zero três vezes, gerando os indicadores zerado, comprado e vendido para os E lógicos.
- Cada E de entrada une três condições — lado da média, extremo do RSI e posição zerada — e aciona um bloco de modificação com a condição Abrir posição; as saídas usam blocos com a condição Fechar posição, que dispensam volume.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
