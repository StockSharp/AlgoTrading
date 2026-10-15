# Estratégia Fibonacci Time Zones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão para StockSharp do expert advisor do MetaTrader "Fibonacci Time Zones". Ela preserva o caráter discricionário do script original ao combinar um filtro MACD de período mais alto com saídas por bandas de Bollinger e um módulo rico de gestão de dinheiro. Todas as rotinas de gestão das operações foram reescritas usando a API de alto nível: a estratégia assina dois fluxos de candles (um período de negociação e um período mais lento para confirmação MACD) e vincula indicadores diretamente por callbacks `Bind`/`BindEx`.

## Lógica central

1. **Filtro de momentum** - Um histograma MACD mensal (configurável) é calculado. Um cruzamento altista acima da linha de sinal agenda entradas compradas, enquanto um cruzamento baixista agenda entradas vendidas. A posição real é aberta no próximo candle de negociação para evitar ordens repetidas no mesmo cruzamento.
2. **Execução da entrada** - Cada sinal envia um número de ordens a mercado definido pelo usuário. A exposição oposta existente é zerada antes de abrir uma nova posição.
3. **Regras de saída** - Múltiplas camadas de defesa são aplicadas:
   - **Saída por banda de Bollinger**: posições compradas são fechadas quando o preço toca a banda superior; posições vendidas, quando a banda inferior é atingida.
   - **Stop/alvo clássico**: distâncias estáticas de stop-loss, take-profit e trailing-stop são convertidas de pips para unidades de preço e passadas para `StartProtection`.
   - **Break-even**: depois que o preço percorre um número configurável de pips, o stop é puxado para o break-even mais um offset. Se o preço recuar até esse nível, a posição é fechada.
   - **Trailing monetário**: PnL aberto e realizado são monitorados. Quando o lucro flutuante atinge um limite, a estratégia começa a acompanhá-lo e fecha tudo depois de um drawdown configurável.
   - **Alvos de equity**: alvos opcionais de lucro absoluto ou percentual fecham todas as operações imediatamente quando atingidos.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `UseTakeProfitMoney`, `TakeProfitMoney` | Fecha todas as posições quando o lucro combinado (realizado + não realizado) atinge o valor especificado na moeda da conta. |
| `UseTakeProfitPercent`, `TakeProfitPercent` | Semelhante à opção anterior, mas medido como percentual do equity inicial. |
| `EnableTrailingProfit`, `TrailingTakeProfitMoney`, `TrailingStopLossMoney` | Ativa o trailing baseado em dinheiro assim que o primeiro limite é atingido e protege ganhos acumulados. |
| `UseStop`, `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Stop clássico, alvo e distâncias de trailing expressos em pips. |
| `UseMoveToBreakEven`, `WhenToMoveToBreakEven`, `PipsToMoveStopLoss` | Controla o comportamento de break-even. |
| `NumberOfTrades` | Número de ordens a mercado enviadas para cada sinal (imita o EA original, que podia empilhar entradas). |
| `CandleType`, `MacdCandleType` | Períodos para os candles de gestão e o filtro MACD. |

## Diferenças em relação ao EA original

* O tratamento de botões no gráfico e objetos gráficos de Fibonacci não são reproduzidos; a versão StockSharp foca puramente na execução sistemática.
* O expert original operava por cliques manuais em botões. A versão entra automaticamente em cruzamentos MACD para entregar uma estratégia determinística e testável em backtest.
* Funções de conta específicas do MetaTrader foram substituídas por equivalentes do StockSharp (valores de `Portfolio` e `PnL`).

## Dicas de uso

1. Selecione tipos de candle apropriados antes de iniciar a estratégia. Os padrões correspondem a um gráfico de negociação de 15 minutos com um filtro MACD mensal.
2. Ajuste as distâncias baseadas em pips de acordo com o tamanho do tick do instrumento. A estratégia converte pips para preço internamente usando `Security.PriceStep`.
3. Para intervenção discricionária, desabilite os alvos automáticos de lucro e use apenas a saída por Bollinger.
