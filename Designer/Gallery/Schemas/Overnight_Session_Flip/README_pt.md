# Diagrama da estratégia Overnight Session Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama combina uma SMA formada de 20 períodos com duas janelas programadas para ordens a mercado. O relógio da estratégia avalia o candle finalizado mais recente de cinco minutos, a posição e a data do calendário: uma compra válida pode ser enviada durante a hora 20 e uma venda válida durante a hora 8. Um registro de data limita o diagrama a uma ordem enviada por data do calendário.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos atualizam o preço de fechamento e SimpleMovingAverage com período 20.
- O bloco SMA emite somente valores formados, portanto as decisões programadas aguardam até que o indicador tenha histórico suficiente de candles.
- O bloco Time funciona como relógio de decisão. Cada pulso libera os últimos valores armazenados de fechamento, SMA e posição e depois fornece a hora e os componentes do calendário usados pelos filtros.
- Os dois blocos de ordens a mercado usam volume fixo de 1 sem condição de modificação da posição. Os filtros permitem compra somente com posição menor ou igual a zero e venda somente com posição maior ou igual a zero.
- Uma chave numérica de data é registrada antes de acionar o bloco de ordem. O gráfico mostra candles, valores SMA e os dois fluxos MyTrade; o diagrama não possui proteção nem um bloco de saída separado.

## Regras de entrada e saída

- **Entrada comprada**: Durante Night Hour 20, o fechamento do último candle finalizado está acima da SMA formada, a posição atual é menor ou igual a zero e ainda não foi enviada uma ordem na data atual. O diagrama envia uma compra a mercado com Volume 1.
- **Entrada vendida**: Durante Day Hour 8, o fechamento do último candle finalizado está abaixo da SMA formada, a posição atual é maior ou igual a zero e ainda não foi enviada uma ordem na data atual. O diagrama envia uma venda a mercado com Volume 1.
- **Saída**: Não há ordem de saída dedicada nem ordens de proteção. Uma ordem posterior de tamanho fixo na direção oposta pode reduzir uma posição, fechar uma posição contrária de tamanho igual ou atravessar zero quando o módulo atual é menor que Volume; ela não garante uma reversão completa.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Período de cinco minutos; somente candles finalizados atualizam os valores armazenados de preço e SMA. |
| SMA Period | 20 | Número de candles finalizados usado por SimpleMovingAverage; as decisões exigem uma SMA formada. |
| Night Hour | 20 | Hora do relógio da estratégia em que as condições de compra podem enviar uma ordem. |
| Day Hour | 8 | Hora do relógio da estratégia em que as condições de venda podem enviar uma ordem. |
| Volume | 1 | Quantidade fixa fornecida aos dois blocos de ordens a mercado. |

## Detalhes do diagrama

- O fluxo de [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) alimenta um [Conversor](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) do preço de fechamento e um [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) que fornece somente a SMA formada. Uma [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) com a expressão `a` expõe a SMA como valor numérico.
- [Hora atual](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) fornece o carimbo de tempo da estratégia ou da mensagem. A cada pulso, ela aciona blocos de [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) que guardam os últimos valores de fechamento, SMA e posição, portanto Time participa diretamente de cada decisão.
- Os conversores de tempo extraem Hour, Year e DayOfYear. A [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) de data calcula `Year * 1000 + DayOfYear` e gera uma chave estável para cada data do calendário.
- Blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) avaliam as duas horas programadas, o fechamento diante da SMA, a posição diante de zero e a chave da data atual diante da última chave registrada. Dois blocos de [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinam os filtros de compra e venda.
- A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual é amostrada em cada pulso do relógio. A compra exige `Position <= 0` e a venda exige `Position >= 0`.
- Um sinal combinado verdadeiro primeiro registra a chave da data atual e depois aciona seu bloco [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html). Essa ordem impede outra ordem enviada nos pulsos posteriores da mesma data do calendário.
- Os dois blocos Modify position recebem o valor fixo Volume compartilhado e colocam ordens a mercado. O painel Chart recebe candles finalizados, o fluxo da SMA formada e as saídas MyTrade dos blocos de compra e venda.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
