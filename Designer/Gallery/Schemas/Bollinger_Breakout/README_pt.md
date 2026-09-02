# Diagrama da estratégia de rompimento por zonas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O nome fala em rompimento, mas o que se opera é a volta: o diagrama espera um candle cuja zona inferior tenha furado a banda inferior de Bollinger enquanto o mercado ainda se mantém acima da EMA 50, e compra essa queda. A imagem espelhada vende um pico acima da banda superior. A posição é entregue assim que o preço volta à banda do meio. A confirmação por RSI do código original (abaixo de 45 para compras e acima de 55 para vendas) foi deixada de fora para manter o diagrama legível: ela quase não restringe um sinal que já exige um candle além da banda.

![schema](schema.svg)

## Visão geral da estratégia

- As bandas de Bollinger (20, 1.5) marcam a borda esticada da faixa em candles de 30 minutos, e a EMA 50 diz de que lado da tendência o mercado está.
- Em vez de comparar um único preço com a banda, o diagrama constrói uma zona de penetração a partir do próprio candle: 30% da amplitude medidos para cima a partir da mínima nas compras e para baixo a partir da máxima nas vendas.
- As entradas só acontecem com posição zerada, e a banda do meio é a única saída para as duas direções.

## Regras de entrada e saída

- **Entrada comprada**: A zona mínima + 30% da amplitude do candle fica abaixo da banda inferior de Bollinger, o candle é de baixa (fechamento abaixo da abertura), o fechamento está acima da EMA 50 e a posição está zerada. Compra-se um lote a mercado.
- **Entrada vendida**: A zona máxima - 30% da amplitude do candle fica acima da banda superior de Bollinger, o candle é de alta (fechamento acima da abertura), o fechamento está abaixo da EMA 50 e a posição está zerada. Vende-se um lote a mercado.
- **Saída**: Uma compra é encerrada no primeiro candle que fecha na banda do meio ou acima dela, e uma venda no primeiro candle que fecha na banda do meio ou abaixo; ambas as saídas usam blocos de fechamento de posição, portanto cada uma age apenas sobre o lado realmente aberto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Length | 20 | Período de suavização das bandas de Bollinger. |
| Bollinger Width | 1.5 | Multiplicador do desvio padrão das bandas; 1.5 as mantém estreitas, de modo que os candles as alcançam com frequência. |
| EMA Length | 50 | Período da EMA que decide o lado da tendência. |
| Candle Zone, share of range | 0.3 | Fração da amplitude do candle que precisa ficar além da banda para contar como penetração. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:30:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Quatro blocos conversores extraem abertura, máxima, mínima e fechamento do candle; outros três leem as bandas superior, inferior e do meio.
- Dois blocos de fórmula montam as zonas de penetração, mínima + (máxima - mínima) * fração e máxima - (máxima - mínima) * fração, a partir de uma mesma constante.
- Cada E lógico junta quatro sinalizadores: a zona além da banda, a direção do candle, o lado da EMA e a posição zerada obtida pela comparação do bloco de posição com zero.
- O par de comparações de saída confronta o fechamento com a banda do meio e aciona dois blocos de fechamento, liberando o diagrama para o próximo sinal.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
