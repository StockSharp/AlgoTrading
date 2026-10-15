# Diagrama de estratégia de ordem pendente por pin bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama somente comprado procura uma sombra inferior profunda dentro de um leque ascendente de médias. Em vez de comprar no fechamento do sinal, coloca uma ordem limitada dentro da sombra, cancela a ordem não executada após um número fixo de velas concluídas, protege a execução com saídas percentuais e também fecha quando a EMA rápida cai abaixo da média.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de trinta minutos alimentam conversores de abertura, máxima, mínima e fechamento, além de EMA 6, EMA 18 e SMA 50 formadas.
- Formula calcula (min(open, close) - low) / (high - low). A sombra inferior deve superar 0,45 da amplitude total.
- O filtro exige EMA 6 > EMA 18 > SMA 50. A mínima precisa perfurar EMA 6 e o fechamento voltar acima dela.
- O portão de entrada reúne o padrão, Position zerada e pausa de seis velas desde a última execução da estratégia.
- Order registering coloca compra limitada de Order Volume em low * (1 + 0,25 / 100), sem arredondamento de preço.
- N values conta seis velas concluídas desde o registro e aciona Order cancellation se a ordem continuar ativa; Trades for order envia as execuções à proteção.
- Position protection coloca take-profit de 1,4% e stop-loss de 0,7%; EMA 6 abaixo de EMA 18 também aciona ClosePosition a mercado.

## Regras de entrada e saída

- **Entrada comprada**: A vela atende quando a sombra inferior supera 0,45, EMA 6 > EMA 18 > SMA 50, a mínima fica abaixo de EMA 6, o fechamento volta acima, Position está zerada e passaram seis velas desde a última execução. O diagrama registra compra limitada 0,25% acima da mínima; a entrada só ocorre se o preço posterior a executar.
- **Entrada vendida**: Não há entrada vendida. O enfraquecimento do leque é condição de saída, não sinal para abrir posição curta.
- **Saída**: Uma limitada não executada é cancelada após seis velas. Uma posição comprada fecha pela Position protection em +1,4% ou -0,7%, ou por ClosePosition a mercado quando EMA 6 cai abaixo de EMA 18. A execução da saída retorna à proteção para limpar seu estado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:30:00 | Período das velas concluídas usado pelo padrão, indicadores, vida, pausa e saídas. |
| Fast EMA Length | 6 | Comprimento da ExponentialMovingAverage rápida perfurada pela sombra e recuperada pelo fechamento. |
| Medium EMA Length | 18 | Comprimento da ExponentialMovingAverage média no leque. |
| Slow SMA Length | 50 | Comprimento da SimpleMovingAverage lenta na base do leque. |
| Wick Share | 0.45 | Participação mínima da sombra inferior na amplitude completa. |
| Entry Offset, % | 0.25 | Percentual acima da mínima de sinal usado na compra limitada. |
| Order Volume | 1 | Quantidade de cada compra pendente. |
| Order Life, candles | 6 | Velas concluídas antes de cancelar uma limitada não executada. |
| Take Profit, % | 1.4 | Distância favorável da execução de entrada, em percentual. |
| Stop Loss, % | 0.7 | Distância adversa da execução de entrada, em percentual. |
| Cooldown, candles | 6 | Mínimo de velas concluídas desde a última execução antes de nova entrada. |

## Detalhes do diagrama

- Preços, indicadores, estados e contadores usam uma única série concluída de trinta minutos, mantendo as ordens no relógio de negociação.
- O portão AND combina sombra, duas comparações do leque, perfuração e recuperação da EMA rápida, Position zerada e pausa pronta.
- Order registration envia a mesma ordem a N values, Order cancellation, Trades for order e gráfico; o contador começa no registro e avança com velas concluídas.
- Strategy trades zera a pausa em cada execução própria; cada vela a incrementa até o limite, e um valor inicial alto permite o primeiro setup imediatamente.
- A posição zerada bloqueia novas entradas após uma execução. Enquanto uma limitada anterior segue pendente, outra vela válida forma uma tentativa independente com o mesmo cancelamento em seis velas.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
