# Diagrama da estratégia Lunch Break Fade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia a reversão do movimento de curto prazo durante a janela de almoço das 11:00:00 às 14:59:59 com candles finalizados de cinco minutos. Ele entra somente a partir de uma posição zerada, sai comparando o fechamento com uma SMA formada de 20 períodos e bloqueia todos os caminhos de entrada e saída pelos próximos 30 candles concluídos após um sinal de ordem.

![schema](schema.svg)

## Visão geral da estratégia

- Somente candles finalizados de cinco minutos entram na cadeia de decisão. A SMA começa a emitir após seu aquecimento de 20 períodos, e dois blocos Previous value fornecem os dois fechamentos imediatamente anteriores.
- O bloco Working time lê o horário de abertura de cada candle e permite entradas das 11:00:00 às 14:59:59, incluindo os dois limites. A janela de horário não restringe as saídas.
- Dois fechamentos anteriores ascendentes seguidos por um candle atual baixista geram uma entrada vendida a partir de uma posição zerada. Dois fechamentos descendentes seguidos por um candle altista geram uma entrada comprada.
- Uma posição comprada sai quando o fechamento está abaixo da SMA, e uma posição vendida sai quando o fechamento está acima da SMA. Quatro caminhos separados enviam ordens a mercado de volume fixo para as duas entradas e as duas saídas.
- Cada sinal de entrada ou saída ativa um intervalo que bloqueia os dois tipos de ação pelos próximos 30 candles concluídos. Não há bloco de proteção de posição; o gráfico mostra candles, SMA e execuções dos quatro caminhos de ordens.

## Regras de entrada e saída

- **Entrada comprada**: Durante a janela de almoço, quando `Close[-1] < Close[-2]`, o candle atual é altista (`Close > Open`), o retrato da posição é zero e o intervalo terminou, o diagrama envia uma compra a mercado com Volume 1.
- **Entrada vendida**: Durante a janela de almoço, quando `Close[-1] > Close[-2]`, o candle atual é baixista (`Close < Open`), o retrato da posição é zero e o intervalo terminou, o diagrama envia uma venda a mercado com Volume 1.
- **Saída**: Com o intervalo encerrado, uma posição comprada envia uma venda a mercado quando `Close < SMA`, enquanto uma posição vendida envia uma compra a mercado quando `Close > SMA`. Essas verificações de nível funcionam dentro e fora da janela de almoço. Nenhum stop-loss, take-profit ou outra proteção está conectado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Período de cinco minutos; somente candles finalizados acionam o indicador, o histórico, o intervalo e as decisões. |
| SMA Period | 20 | Período da SimpleMovingAverage usado pelas duas verificações de nível de saída. |
| Cooldown Bars | 30 | Quantidade de candles concluídos posteriores durante os quais os sinais de entrada e saída ficam bloqueados. |
| Lunch Begin | 11:00:00 | Limite inclusivo do horário de abertura a partir do qual as entradas do almoço ficam habilitadas. |
| Lunch End | 14:59:59 | Limite inclusivo do horário de abertura até o qual as entradas do almoço permanecem habilitadas. |
| Volume | 1 | Quantidade fixa fornecida aos quatro blocos de ordens a mercado com NoCondition. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles finalizados de cinco minutos. Um [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) limitado a valores formados calcula SimpleMovingAverage 20, e uma [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) disponibiliza seu valor numérico. A porta [Negociação permitida](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) libera o candle armazenado na cadeia de decisão.
- Blocos [Conversor](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) extraem os preços Close e Open. Dois blocos [Valor anterior](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) usam deslocamentos 1 e 2; uma porta de histórico pronto impede decisões até que os dois fechamentos anteriores estejam disponíveis.
- O bloco [Horário de trabalho](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) recebe diretamente o fluxo de candles e compara os metadados do horário de abertura com os limites inclusivos da janela de almoço. Seu resultado participa somente das duas condições de entrada.
- A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual é guardada em uma [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) e emitida uma vez por candle de decisão. Blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) e [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinam sessão, direção anterior, direção do candle, posição, histórico pronto, nível da SMA e estado do intervalo.
- Um bloco [Atrasar sinal](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) conta 30 candles finalizados posteriores. Variáveis de estado bloqueiam as quatro condições de ação durante a contagem e voltam a habilitá-las no candle seguinte.
- Quatro blocos [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocam ordens a mercado com `NoCondition` e Volume 1 compartilhado: entrada comprada, entrada vendida, saída por venda da posição comprada e saída por compra da posição vendida. Não há elemento de proteção.
- O [Painel de gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe candles finalizados, o fluxo formado da SMA e a saída MyTrade de cada um dos quatro blocos de ordens.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
