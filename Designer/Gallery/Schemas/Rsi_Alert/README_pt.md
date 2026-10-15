# Diagrama da estratégia de alerta RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama transforma os extremos do RSI em operações e alertas legíveis. Ele processa candles finalizados de cinco minutos, compra em 30 ou abaixo e vende em 70 ou acima somente quando a posição está zerada, e aplica proteção percentual a cada entrada executada. Cada sinal aceito também captura o valor numérico do RSI, formata-o e registra uma notificação.

![schema](schema.svg)

## Visão geral da estratégia

- Um único fluxo de candles finalizados de cinco minutos alimenta o indicador, o snapshot da posição, as decisões de entrada, as verificações do preço de proteção e o gráfico.
- RelativeStrengthIndex usa um período de 14. Seu filtro de valores formados está desativado (`IsFormed = false`), portanto os valores do período de aquecimento não são suprimidos apenas porque o indicador ainda não está formado.
- Um bloco Formula com a expressão `a` converte o IndicatorValue do RSI em um valor decimal usado pelas comparações e mensagens.
- O RSI decimal é comparado com os níveis de sobrevenda e sobrecompra. O sinal de cada direção é combinado com um snapshot da posição obtido durante a avaliação do candle atual, e ambos os blocos de entrada usam a condição Open position.
- As entradas executadas ativam a proteção da posição com take profit de 2% e stop loss de 1%. Os sinais de entrada aceitos também enviam o valor RSI capturado por um formatador até uma notificação Log.

## Regras de entrada e saída

- **Entrada comprada**: O RSI decimal está no Oversold Level ou abaixo e o snapshot da posição indica que ela está zerada. O diagrama compra a mercado o volume configurado e registra um alerta de compra com o valor do sinal.
- **Entrada vendida**: O RSI decimal está no Overbought Level ou acima e o snapshot da posição indica que ela está zerada. O diagrama vende a mercado o volume configurado e registra um alerta de venda com o valor do sinal.
- **Saída**: A proteção da posição fecha a operação quando o fechamento de um candle finalizado atinge o nível de take profit de 2% ou o nível de stop loss de 1% em relação à entrada. Um sinal RSI oposto não reverte uma posição aberta, e uma execução protetora não pode provocar outra entrada no mesmo candle.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| RSI Period | 14 | Número de candles usados para calcular RelativeStrengthIndex. |
| Oversold Level | 30 | Valores RSI neste nível ou abaixo permitem uma entrada comprada enquanto o diagrama está sem posição aberta. |
| Overbought Level | 70 | Valores RSI neste nível ou acima permitem uma entrada vendida enquanto o diagrama está sem posição aberta. |
| Take Profit | 2% | Distância protetora do take profit em relação ao preço de entrada. |
| Stop Loss | 1% | Distância protetora do stop loss em relação ao preço de entrada. |
| Volume | 0.01 | Volume da ordem de entrada, em lotes. |
| Candles | 00:05:00 | Período dos candles de cinco minutos; somente candles finalizados são processados. |

## Detalhes do diagrama

- A saída de [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) primeiro aciona o snapshot da posição atual, depois atualiza o bloco [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) e, por fim, atualiza um [Conversor](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) do preço de fechamento.
- A saída do RSI entra em um bloco [Formula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) cuja expressão é `a`. Sua saída decimal chega às duas memórias do valor da mensagem antes que qualquer comparação de limiar seja avaliada.
- Dois blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) testam o RSI decimal em relação aos valores compartilhados Oversold Level e Overbought Level usando `<=` e `>=`.
- O valor de [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) é mantido por uma [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) durante a avaliação do candle atual e comparado com zero. Dois blocos de [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinam esse resultado de posição zerada com os sinais RSI de compra e venda.
- Ambos os blocos de entrada [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) usam ordens a mercado com a condição Open position e recebem `0.01` de um único valor de volume compartilhado.
- As saídas MyTrade de ambos os blocos de entrada alimentam a [Proteção da posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). O conversor do fechamento do candle fornece sua entrada Price, e o bloco usa take profit de 2% e stop loss de 1%.
- Cada sinal de entrada combinado aciona sua própria memória do valor RSI. [Formatação de texto](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) gera `RSI {0:0.0} <= 30 — buy` ou `RSI {0:0.0} >= 70 — sell`, e os blocos de [Notificação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) do tipo Log publicam as mensagens.
- O painel do gráfico recebe candles finalizados, valores RSI, ambos os fluxos de operações de entrada e as operações de saída protetoras.

## Uso

Importe o arquivo `.json` no Designer e execute-o sobre dados históricos no backtester. Observe no log as notificações RSI formatadas e verifique as saídas protetoras em relação aos fechamentos dos candles. Se alterar qualquer um dos limiares RSI, atualize também seu modelo de formatação para que o texto do alerta permaneça exato.
