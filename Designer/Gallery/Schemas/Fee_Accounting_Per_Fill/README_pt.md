# Diagrama de contabilização de comissão por execução
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia retornos confirmados do CCI(30) horário aos seus níveis com uma ordem a mercado de volume fixo e demonstra a contabilização de comissão para cada execução observada. Um intervalo de quatro candles controla novos sinais, uma camada educativa de proteção percentual pode fechar a posição e um único registro recebe tanto a cobrança calculada por execução quanto a comissão acumulada do motor da estratégia.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de uma hora alimentam CommodityChannelIndex 30. O indicador emite todos os valores, inclusive os produzidos antes de completar seu comprimento.
- Uma compra exige CCI anterior < -100 e CCI atual >= -100. Uma venda exige CCI anterior > 100 e CCI atual <= 100.
- A compra também exige Position <= 0, a venda exige Position >= 0 e ambas requerem que o intervalo de quatro candles esteja pronto.
- Cada sinal aceito envia exatamente uma ordem a mercado de Volume fixo. Com posição zerada ela abre o lado indicado; contra uma posição unitária oposta ela fecha até zero e não abre o outro lado no mesmo sinal.
- A proteção de posição é uma camada educativa explícita com take-profit de 1% e stop-loss fixo de 0,7%. Ela verifica somente fechamentos de candles finalizados e ignora o fechamento de qualquer candle que já tenha produzido uma ordem de sinal.
- Cada execução observada gera uma cobrança simulada por Trade.Price × Trade.Volume × Commission Rate % / 100. O gráfico mostra CCI, ordens, execuções e ambas as séries de comissão, enquanto as mensagens formatadas são gravadas em um único fluxo de registro.

## Regras de entrada e saída

- **Entrada comprada**: Quando o CCI anterior está abaixo de -100, o CCI atual retorna a -100 ou mais, Position <= 0 e o intervalo está pronto, envia-se uma compra a mercado de Volume. Com posição zerada ela abre comprado; contra uma posição vendida unitária ela apenas fecha até zero.
- **Entrada vendida**: Quando o CCI anterior está acima de 100, o CCI atual retorna a 100 ou menos, Position >= 0 e o intervalo está pronto, envia-se uma venda a mercado de Volume. Com posição zerada ela abre vendido; contra uma posição comprada unitária ela apenas fecha até zero.
- **Saída**: Um sinal CCI oposto e permitido pode zerar uma posição unitária com uma ordem a mercado de volume fixo. Independentemente disso, a camada educativa de proteção pode fechar a exposição acompanhada no take-profit de 1% ou stop-loss fixo de 0,7%; sua entrada de preço recebe apenas fechamentos finalizados de candles sem ordem de sinal.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 01:00:00 | Período de uma hora; somente candles finalizados conduzem decisões CCI, incrementos do intervalo e verificações de preço da proteção. |
| CCI Length | 30 | Comprimento de CommodityChannelIndex; o bloco emite valores sem aguardar a formação completa do indicador. |
| Lower Level | -100 | Nível inferior do CCI. O retorno para cima através de -100 cria a condição de compra. |
| Upper Level | 100 | Nível superior do CCI. O retorno para baixo através de 100 cria a condição de venda. |
| Cooldown | 4 | Número de candles finalizados necessários após uma execução antes que outro sinal possa negociar. |
| Commission Rate % | 0.04 | Taxa percentual usada somente pela fórmula exibida por execução `Trade.Price × Trade.Volume × rate / 100`. |
| Take Profit % | 1 | Movimento percentual favorável empregado pela camada educativa de proteção da posição. |
| Stop Loss % | 0.7 | Movimento percentual adverso empregado pelo stop fixo, sem rastreamento, da camada educativa de proteção. |
| Volume | 1 | Quantidade fixa de cada ordem de sinal de compra ou venda. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles horários finalizados. O fechamento fica armazenado para a proteção e um bloco [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calcula CCI 30 com o filtro de valores formados desativado. Flags por candle preservam as quatro comparações do valor anterior e atual com os níveis até o pulso final de decisão.
- [Posição atual](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) fornece as travas Position <= 0 e Position >= 0. O intervalo começa em 4, é incrementado e limitado antes de cada decisão do candle e retorna a 0 em cada execução direta de ordem de sinal ou de proteção. Assim, os candles finalizados 1, 2 e 3 após uma execução são bloqueados e o candle 4 é permitido.
- Dois blocos de [Registro de ordem](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/orders/register.html) enviam compra e venda a mercado com Volume fixo. A saída direta de execução de cada bloco atualiza a proteção e reinicia o intervalo; um bloco Trades for order dedicado observa a Order registrada e fornece o fluxo de execuções de sinal para a comissão simulada e o gráfico.
- Os dois fluxos diretos de execuções de sinal entram na [Proteção da posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), fazendo sua posição interna retornar a zero após um fechamento por sinal. Sua própria saída de execução não volta para essa entrada. A trava sem sinal libera o fechamento finalizado armazenado para verificar a proteção somente quando nenhuma ordem de sinal disparou naquele candle.
- Para cada execução observada de compra, venda ou proteção, conversores armazenam Trade.Price e Trade.Volume em travas silenciosas. Em seguida, um pulso libera taxa, preço e volume nessa ordem; a [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) atualiza `a × b × r / 100`, e o estado silencioso da comissão é emitido por último exatamente uma vez para essa execução observada.
- A saída Commission de [Lucros e perdas da estratégia](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) é a comissão acumulada do motor e permanece em zero quando o ambiente de teste ou execução não tem regra de comissão configurada. A fórmula simulada por execução serve apenas para exibição e não grava nesse valor do motor.
- As saídas de dois [Formatadores de texto](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) são reunidas por Combination<IComparable> e enviadas a uma [Notificação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) Log. Trades for order assina após receber a Order registrada; portanto, um ambiente que complete uma ordem dentro da chamada de registro pode produzir uma execução antes da conexão do observador. A saída direta de execução ainda controla proteção e intervalo nesse caso.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
