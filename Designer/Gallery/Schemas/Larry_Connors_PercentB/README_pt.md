# Diagrama da estratégia Bollinger %B de Larry Connors
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama de reversão à média somente comprado, construído sobre o Bollinger %B — a posição do fechamento dentro das bandas de Bollinger, expressa como percentual da largura delas. A ideia de Larry Connors é que um único candle fraco não prova nada, então o diagrama espera que o %B permaneça na parte baixa da banda por dois candles seguidos antes de comprar, e segura até que o %B se recupere para a parte alta.

![schema](schema.svg)

## Visão geral da estratégia

- O indicador BollingerPercentB faz em um bloco o que a estratégia original calcula à mão a partir das bandas; sua escala vai de 0 a 100, por isso os limiares clássicos 0.35 e 0.8 aparecem como 35 e 80.
- Um bloco de valor anterior guarda a leitura do candle passado, e é ele que transforma um candle fraco isolado em uma condição de dois candles.
- A estratégia é apenas comprada: compra a fraqueza e vende de volta a mesma compra, nunca abrindo venda.
- A posição participa das duas decisões, de modo que a entrada não se acumula e a saída não dispara sem posição.

## Regras de entrada e saída

- **Entrada comprada**: O %B do candle anterior e o do candle atual estão ambos abaixo do limiar inferior, e a posição não está comprada. A ordem compra um lote.
- **Entrada vendida**: O diagrama nunca vende a descoberto. O bloco de venda serve apenas como saída de uma compra aberta.
- **Saída**: O %B sobe acima do limiar superior enquanto a posição está comprada. A ordem vende esse mesmo lote e zera a posição.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Period | 20 | Período das bandas de Bollinger sobre as quais o %B é calculado. |
| Bollinger Deviation | 2 | Multiplicador do desvio padrão das bandas de Bollinger. |
| Low %B | 35 | Limiar abaixo do qual o %B conta como parte baixa da banda; precisa valer por dois candles seguidos. |
| High %B | 80 | Limiar acima do qual o %B é considerado recuperado, o que encerra a compra. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador, cujo valor segue tanto para as comparações quanto para o bloco de valor anterior.
- Duas comparações com a mesma constante inferior dão a condição do candle atual e a do anterior; uma terceira confronta o %B com a constante superior para a saída.
- Outras duas comparações verificam a posição contra zero: não comprada para a entrada, comprada para a saída.
- Os dois E lógicos acionam os blocos de modificação de posição, que tomam o volume de uma única constante compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
