# Diagrama de ordens pendentes no retorno do CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama negocia o retorno do CCI das zonas extremas com ordens limitadas de vida curta. O preço executável vem do fechamento finalizado, não do bid ou ask de Level 1, preservando o replay sem esses campos.

![schema](schema.svg)

## Visão geral da estratégia

- Candles horários alimentam CCI(30); Previous value distingue o retorno acima de −100 ou abaixo de +100 da permanência na zona extrema.
- Direção de Position, intervalo de quatro candles após execução e trava global de ordem pendente filtram os dois lados.
- Order registering coloca uma única ordem limitada no fechamento sem ajustar o preço.
- A Order registrada arma N values, os candles contam e, após quatro, Order cancellation remove uma ordem não executada.

## Regras de entrada e saída

- **Entrada comprada**: CCI anterior <= −100, atual > −100, Position não comprado, intervalo concluído e nenhuma pendente: compra limitada no fechamento.
- **Entrada vendida**: CCI anterior >= +100, atual < +100, Position não vendido, intervalo concluído e nenhuma pendente: venda limitada no fechamento.
- **Saída**: Não há stop ou alvo fixo. Um retorno oposto do CCI pode enviar ordem contrária e levar Position a zero; ordem não executada é cancelada no vencimento.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 01:00:00 | Time frame dos candles para CCI, intervalo, preço e vida da ordem. |
| CCI Length | 30 | Número de valores no CommodityChannelIndex. |
| CCI Level | 100 | Fronteira extrema simétrica como +Level e −Level. |
| Signal Cooldown, candles | 4 | Candles finalizados após a última execução até nova ordem. |
| Order Volume | 1 | Quantidade de cada ordem limitada pendente. |
| Pending Lifetime, candles | 4 | Máximo de candles finalizados para uma ordem ativa não executada. |

## Detalhes do diagrama

- A pasta mantém o nome da posição da galeria, mas Level 1 não é conectado: close é o preço pendente explícito e reproduzível.
- Uma Order registrada ativa o estado pendente; Finished o limpa em execução, cancelamento ou falha.
- Intervalo e vida da pendente são separados: um mede após execução e o outro limita ordem não executada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
