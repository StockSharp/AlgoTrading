# Diagrama da estratégia de expansão do ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Aqui o sinal é a própria volatilidade. O Average True Range é comparado com o seu valor do candle anterior: quando cresce ao menos na proporção definida, algo começou a se mover, e o diagrama entra nesse movimento no sentido apontado pela média móvel simples. Quando a amplitude encolhe na mesma proporção, o movimento é dado como encerrado e a posição é fechada.

![schema](schema.svg)

## Visão geral da estratégia

- O Average True Range mede o tamanho dos últimos candles, e um bloco de valor anterior guarda a leitura de um candle atrás para que as duas possam ser comparadas.
- Expansão é o ATR igual ou acima do ATR anterior multiplicado pela proporção; contração é a imagem espelhada: o ATR anterior acima do ATR multiplicado pela mesma proporção.
- A média móvel simples decide apenas o lado: com o fechamento acima dela a expansão vira compra, abaixo dela vira venda.
- Os dois blocos de entrada trazem a condição de abertura e os dois de saída a de encerramento, de modo que o diagrama mantém uma única posição e nunca a aumenta.

## Regras de entrada e saída

- **Entrada comprada**: A volatilidade está se expandindo, o candle fecha acima da média móvel simples e a posição está zerada. A ordem compra a mercado o volume compartilhado.
- **Entrada vendida**: A volatilidade está se expandindo, o candle fecha abaixo da média móvel simples e a posição está zerada. A ordem vende a mercado o volume compartilhado.
- **Saída**: A volatilidade se contrai, ou seja, o ATR multiplicado pela proporção cai abaixo do ATR anterior. O lado que estiver aberto é encerrado a mercado pelo bloco correspondente; não há stop loss nem realização de lucro, exatamente como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| ATR Period | 14 | Período de suavização do Average True Range que mede a volatilidade. |
| MA Period | 20 | Período da média móvel simples que decide a direção da entrada. |
| Expansion ratio | 1.05 | Quanto o novo ATR precisa superar o anterior para contar como expansão; o seu inverso é o limiar de contração que fecha a posição. |
| Volume | 1 | Volume da ordem, em lotes, compartilhado pelos dois blocos de entrada. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o ATR, a média móvel e um conversor que lê o preço de fechamento.
- Um bloco de valor anterior guarda o ATR do candle precedente, e dois blocos de fórmula aplicam a proporção: um monta o nível de expansão, o outro o de contração.
- Dois blocos de comparação transformam esses níveis em sinalizadores de expansão e contração, e outros dois colocam o fechamento diante da média móvel.
- Cada E lógico junta volatilidade, direção e a comparação da posição com zero, e aciona um dos dois blocos de entrada; o sinalizador de contração sozinho aciona os dois blocos de encerramento, cuja direção define qual lado podem fechar.
- Duas coisas do original em C# não foram trazidas: a pausa de quinhentos candles após cada operação, que não tem bloco equivalente, e os candles de um minuto, substituídos pelos de cinco minutos do histórico que acompanha a galeria.
- O parâmetro Lookback do original também ficou de fora, porque o código nunca o lê.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
