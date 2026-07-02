# Estratégia de testador de índices
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Indices Tester Strategy** é uma versão direta do MetaTrader 5 consultor especialista "Indices Tester". O sistema concentra-se na negociação de índices intradiários, onde uma única posição longa é aberta durante uma janela de negociação muito estreita. As decisões de negociação dependem puramente de filtros de tempo e limites operacionais:

- Um único fluxo de velas configurável aciona o relógio interno da estratégia.
- Novas posições só podem ser abertas entre os horários de início e término da sessão configurados.
- É permitido um número fixo de negociações por dia, evitando reentradas repetidas.
- Todas as posições abertas são fechadas à força num momento de liquidação definido.
- A estratégia opera apenas no lado comprado, espelhando o consultor especialista original.

Esta implementação usa o StockSharp API de alto nível, assina dados de velas com `SubscribeCandles` e lida com decisões de negociação no retorno de chamada `ProcessCandle`. Não são necessários indicadores, mantendo a lógica enxuta e focada no tempo e nos controles de risco.

## Lógica de negociação
1. **Reinicialização diária** – a estratégia acompanha o dia de negociação atual. Quando um novo dia começa, todos os contadores são zerados, permitindo uma nova margem de negociação para aquele dia.
2. **Janela de entrada** – somente velas com tempo de fechamento estritamente dentro do intervalo `[SessionStart, SessionEnd)` podem acionar entradas. Isso reproduz as verificações `TimeStart` e `TimeEnd` do código original.
3. **Limites de posições e negociações** – as entradas serão ignoradas se o número de negociações já abertas durante o dia atual atingir `DailyTradeLimit` ou se o número de posições abertas simultaneamente exceder `MaxOpenPositions`.
4. **Envio de ordem** – quando todas as condições estão alinhadas, a estratégia envia uma ordem de compra de mercado para `TradeVolume` unidades. O contador de negociações do dia é incrementado imediatamente após o envio da ordem.
5. **Saída forçada** – se uma vela fechar após `CloseTime` e houver uma posição longa ativa, a estratégia fecha a posição com uma ordem de venda a mercado. Isso reflete a lógica do temporizador `ClosePos()` da implementação MQL.

A combinação do contador de negociações e do limitador de posições garante que o sistema se comporte como um simples agendador de negociação única por dia por padrão, ao mesmo tempo que permite o ajuste de parâmetros para atividades mais frequentes.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de velas primárias que aciona o relógio estratégico (o padrão é velas de 1 minuto). |
| `SessionStart` | Hora do dia em que novas negociações podem começar. |
| `SessionEnd` | Hora do dia em que novas negociações não são mais permitidas. |
| `CloseTime` | Hora do dia em que qualquer posição aberta restante é liquidada. |
| `DailyTradeLimit` | Número máximo de entradas permitidas por dia antes da suspensão da negociação. |
| `MaxOpenPositions` | Número máximo de posições longas abertas simultaneamente (contadas em unidades de negociação). |
| `TradeVolume` | Volume de ordens de mercado utilizado para cada entrada. |

## Notas e diferenças
- StockSharp não expõe tabelas de sessão MetaTrader, portanto, a conversão depende do tempo de troca dos carimbos de data e hora da vela junto com a proteção `IsFormedAndOnlineAndAllowTrading()`.
- O consultor especialista original usava temporizadores de segundo nível; esta porta aproveita o fechamento de velas para impulsionar o tempo de entrada e as saídas forçadas, o que é suficiente para janelas de negociação de nível minuto.
- As contagens de negociação são redefinidas no início de cada dia de negociação detectada a partir dos horários de fechamento das velas, mantendo o comportamento consistente em diferentes fusos horários, desde que a origem da vela corresponda à bolsa desejada.

## Dicas de uso
- Certifique-se de que o `CandleType` configurado corresponda ao mercado que está sendo negociado para que os filtros de tempo se alinhem com a sessão desejada.
- Aumente `DailyTradeLimit` se forem necessárias várias tentativas por dia, por exemplo, ao executar em intervalos de tempo mais curtos.
- Defina `MaxOpenPositions` acima de `1` somente quando o escalonamento parcial em posições for desejado; caso contrário, mantenha o padrão para imitar exatamente o script MetaTrader.
