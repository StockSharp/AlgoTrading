# Diagrama da estratégia de rompimento do canal de Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um canal de Keltner é uma média móvel exponencial com bordas afastadas por um múltiplo do Average True Range. O diagrama espera um fechamento fora de uma borda dentro da qual o fechamento anterior ainda estava e vira toda a posição no sentido do rompimento. Não há stop nem alvo: é o rompimento contrário que retira a operação.

![schema](schema.svg)

## Visão geral da estratégia

- KeltnerChannels produz o canal em um único bloco e dois conversores extraem do seu valor a borda superior e a inferior.
- Blocos de valor anterior guardam as duas bordas e o fechamento de uma barra atrás, de modo que o rompimento é medido contra o nível que o mercado já viu e não contra uma borda que se moveu junto com o mesmo candle.
- Cada ordem leva o volume compartilhado mais o módulo da posição, então uma única ordem inverte a operação em vez de apenas reduzi-la.
- O original em C# usa um canal de período 500 com multiplicador 10 em candles de um minuto; o diagrama adota o canal 20 / 2 documentado no seu README em candles de cinco minutos, para que o rompimento realmente aconteça em dados comuns.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está acima da banda superior do candle anterior enquanto o fechamento anterior ainda estava nela ou abaixo, e a posição não está comprada. A ordem compra o volume mais a venda aberta, invertendo para compra.
- **Entrada vendida**: O fechamento está abaixo da banda inferior do candle anterior enquanto o fechamento anterior ainda estava nela ou acima, e a posição não está vendida. A ordem vende o volume mais a compra aberta, invertendo para venda.
- **Saída**: Não há bloco de saída: o rompimento contrário inverte a posição, exatamente como na estratégia original, que não tem stop nem alvo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Channel period | 20 | Período do canal de Keltner; define tanto a média móvel quanto a amplitude de que sai a largura. |
| ATR multiplier | 2 | Quantas amplitudes separam as bordas do canal da linha central. |
| Volume | 1 | Volume da ordem, em lotes, antes de somar a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o indicador e um conversor que lê o preço de fechamento.
- Três blocos de valor anterior deslocam a banda superior, a inferior e o fechamento em uma barra; o indicador só emite quando formado, então as primeiras barras são puladas sozinhas.
- Quatro blocos de comparação formam cada lado do rompimento: um para o candle que sai e outro para o que ainda estava dentro.
- A posição é comparada com a constante zero e entra nos dois E lógicos, enquanto um bloco de fórmula soma o seu módulo à constante de volume para dimensionar a ordem de inversão.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
