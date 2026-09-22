# Diagrama de kill switch por PnL flutuante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama envolve um sinal de retorno do CCI com uma camada de emergência monetária. As entradas aparecem como ordens limitadas no fechamento horário; quando o resultado não realizado atinge um limite, todas as ordens ativas são canceladas antes de zerar a posição a mercado.

![schema](schema.svg)

## Visão geral da estratégia

- Candles horários finalizados alimentam um CommodityChannelIndex de 30 períodos e o fechamento fornece o preço limite.
- A compra exige CCI anterior abaixo de −100 e atual de volta a −100 ou mais; a venda espelha o retorno de cima de +100.
- Comprar requer Position <= 0 e vender Position >= 0, portanto um sinal oposto reduz a exposição existente.
- Cada execução reinicia um intervalo de quatro candles; o contador limitado precisa atingir novamente o valor.
- Order registering coloca a ordem no fechamento do candle finalizado sem ajustar o preço calculado.
- P&L change compara o resultado não realizado com +300 e −200; qualquer limite aciona Mass order cancellation e ClosePosition.

## Regras de entrada e saída

- **Entrada comprada**: O CCI anterior estava abaixo de −100, o atual voltou a pelo menos −100, Position não está comprado e quatro candles passaram desde a última execução. Uma compra limitada é colocada no fechamento.
- **Entrada vendida**: O CCI anterior estava acima de +100, o atual voltou a no máximo +100, Position não está vendido e o intervalo terminou. Uma venda limitada é colocada no fechamento.
- **Saída**: Não há alvo nem stop baseado em preço. Com lucro não realizado de 300 ou perda de −200, o kill switch cancela as ordens ativas e envia simultaneamente ClosePosition a mercado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 01:00:00 | Time frame dos candles finalizados usados pelo CCI, intervalo e preços limite. |
| CCI Length | 30 | Número de valores horários no CommodityChannelIndex. |
| CCI Level | 100 | Limite absoluto do CCI usado como +Level e −Level. |
| Signal Cooldown, candles | 4 | Candles finalizados exigidos após a última execução. |
| Order Volume | 1 | Quantidade de cada ordem limitada de entrada. |
| Target Profit, money | 300 | Lucro não realizado em moeda da conta que aciona a liquidação. |
| Cut Loss, money | -200 | Limite de perda não realizada em moeda da conta, normalmente negativo. |

## Detalhes do diagrama

- Previous value guarda o CCI anterior, então a entrada representa retorno pelo nível e não uma condição repetida na zona extrema.
- O intervalo começa em uma execução real, não no sinal ou tentativa de registro, e é limitado a quatro.
- Entradas limitadas no fechamento mostram de propósito o ciclo de ordem pendente e dão função real ao cancelamento em massa.
- Alvo e perda são valores absolutos de PnL não realizado na moeda da conta, uma camada de emergência e não proteção percentual.
- Sem filtros Security ou Portfolio, o cancelamento cobre todas as ordens da estratégia; Modify position fecha depois o lado restante.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
