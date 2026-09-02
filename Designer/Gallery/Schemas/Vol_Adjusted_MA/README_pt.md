# Diagrama da estratégia de média móvel ajustada pela volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama envolve uma média móvel simples num canal cuja semilargura vale alguns Average True Range: quando o mercado fica nervoso as bordas se afastam e quando ele acalma elas se aproximam. Um fechamento fora de uma borda é tratado como rompimento verdadeiro, e a operação é devolvida assim que o preço retorna à média.

![schema](schema.svg)

## Visão geral da estratégia

- SimpleMovingAverage traça a linha central e AverageTrueRange define a que distância ficam as bordas, de modo que o canal acompanha a volatilidade do momento.
- Dois blocos de fórmula montam as bordas como SMA + multiplicador * ATR e SMA - multiplicador * ATR a partir das mesmas três fontes.
- A entrada só ocorre a partir da posição zerada e a única saída é o fechamento voltando a cruzar a linha central; não há stop nem alvo, como no original em C#.
- Dois desvios do original: a pausa de 500 barras após cada operação não é reproduzida, então o diagrama negocia com mais frequência, e o candle de trabalho é de cinco minutos em vez de um, que é o histórico incluído.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está acima da borda superior SMA + multiplicador * ATR e a posição está zerada. O bloco de modificação compra a mercado o volume compartilhado.
- **Entrada vendida**: O fechamento está abaixo da borda inferior SMA - multiplicador * ATR e a posição está zerada. O bloco de modificação vende a mercado o volume compartilhado.
- **Saída**: A compra é devolvida no primeiro candle que fecha abaixo da SMA e a venda no primeiro que fecha acima dela; os blocos de encerramento só agem quando há algo a encerrar.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período da média móvel simples que forma a linha central e o nível de saída. |
| ATR Length | 14 | Período do Average True Range que mede a volatilidade atual. |
| ATR multiplier | 2 | Quantos ATR separam as bordas do canal da linha central. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois indicadores e um conversor que extrai o preço de fechamento.
- Dois blocos de fórmula combinam a média, a amplitude e a constante multiplicadora nas bordas superior e inferior.
- Quatro blocos de comparação formam os sinais: dois contra as bordas do canal para as entradas e dois contra a linha central para as saídas.
- O bloco de posição, comparado com a constante zero, entra em cada E lógico, de modo que nenhuma ordem aumenta uma posição já aberta.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
