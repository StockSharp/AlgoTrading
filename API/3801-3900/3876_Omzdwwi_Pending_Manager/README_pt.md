# Estratégia de gerente pendente de Omzdwwi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Omzdwwi Pending Manager Strategy** é uma tradução direta de alto nível StockSharp do MetaTrader 4 especialista `omzdwwi7739cyjayvs_1_65.mq4`. O consultor original concentra-se em manter um anel de ordens pendentes em torno do preço de mercado atual, executando entradas de mercado em um cronômetro programado e gerenciando trailing stops para posições ativas e ordens pendentes pendentes. Esta versão C# reproduz a mesma lógica enquanto aproveita `Strategy` API, `SubscribeLevel1` feed e auxiliares de gerenciamento de pedidos (`BuyStop`, `SellLimit`, `ReRegisterOrder`, etc.).

A estratégia continuamente:

- Mantém até quatro ordens pendentes (stop de compra, stop de venda, limite de compra, limite de venda) em distâncias configuráveis das cotações de compra/venda.
- Opcionalmente, dispara ordens de compra/venda de mercado em uma hora e minuto específicos.
- Aplica múltiplas camadas de saídas para posições de mercado: take-profit fixo, stop-loss fixo, meta adicional de "lucro em pips" e lógica de trailing stop que imita a rotina `TrailingPositions()` do especialista.
- Move as ordens pendentes para mais perto ou para mais longe do preço de acordo com as regras `TrailingOtlozh()` do especialista, uma vez que o mercado avança pela distância final configurada.
- Monitora os limites de lucros e perdas no nível da conta, emitindo registros de informações/avisos quando as porcentagens globais configuradas de take-profit ou stop-loss são atingidas.

## Fluxo de sinal e assinaturas de dados

- `SubscribeLevel1()` fornece atualizações de lance/venda. Cada atualização de cotação aciona verificações de tempo, colocação de pedidos, ajustes posteriores e verificações de saída. Nenhum dado ou indicador de vela é necessário.
- `GetWorkingSecurities()` declara a assinatura de nível 1 para que a estratégia possa ser executada em ambientes ativos e de backtesting.

## Lógica de entrada

1. **Ordens de mercado programadas.** Quando `UseTimeSignals` está ativado e o relógio do servidor atinge `SignalHour:SignalMinute`, a estratégia gera travas booleanas derivadas dos parâmetros `Time*Signal`. A próxima atualização de nível 1 chama `BuyMarket()` ou `SellMarket()` desde que `WaitClose`/`MaxMarketOrders` permita. As travas são reiniciadas imediatamente após a negociação.
2. **Pedidos pendentes persistentes.** Para cada tipo de pedido ativado (`EnableBuyStop`, `EnableSellStop`, `EnableBuyLimit`, `EnableSellLimit`) a estratégia verifica se há um pedido ativo. Os pedidos ausentes são colocados a `Distance * PriceStep` pontos do melhor lance/venda, replicando o comportamento do especialista `UstanOtlozh()`. Se a ordem já existir, `ReRegisterOrder` mantém o preço alinhado às cotações atuais.

## Lógica de saída para posições de mercado

- **Stop-loss/take-profit fixos** vêm de `MarketStopLossPoints` e `MarketTakeProfitPoints`. Quando a melhor oferta/venda ultrapassa esses limites, a posição é achatada por meio de ordem de mercado.
- **Alvo de pips adicional** replica o comportamento `PipsProfit` do especialista. Quando diferente de zero, fecha a posição após obter o lucro configurado, mesmo que o TP esteja desabilitado.
- **Trailing stop** copia `TrailingPositions()`. Assim que a posição for suficientemente lucrativa (ou imediatamente se `RequireProfitBeforeTrailing=false`), o preço final interno é atualizado para `Bid - MarketTrailingOffsetPoints * PriceStep` para posições longas e `Ask + MarketTrailingOffsetPoints * PriceStep` para posições curtas com o passo de trilha mínimo aplicado por `MarketTrailingStepPoints`.

## Lógica de rastreamento para pedidos pendentes

- As ordens Stop usam `StopTrailingOffsetPoints` e `StopTrailingStepPoints`. Quando o preço ultrapassa o limite MQL (`Ask < OrderPrice - (offset + step)` para paradas de compra, simétrico para vendas), o pedido é registrado novamente em `Ask + offset` ou `Bid - offset`.
- As ordens limitadas usam `LimitTrailingOffsetPoints` e `LimitTrailingStepPoints` da mesma maneira, recriando os ajustes `TrailingOtlozh()`.

## Monitoramento de riscos e contas

- `MaxMarketOrders` limita quantos lotes (expressos em múltiplos de `OrderVolume`) podem ser acumulados por direção quando `WaitClose=false`.
- `UseGlobalLevels`, `GlobalTakeProfitPercent` e `GlobalStopLossPercent` observam o patrimônio do portfólio. Quando os limites são excedidos, a estratégia grava um log de informações ou avisos, espelhando os pop-ups de alerta originais.

## Parâmetros

| Grupo | Parâmetro | Descrição |
|-------|-----------|-------------|
| Geral | `OrderVolume` | Volume de negociação (lotes) reutilizado por cada pedido. |
| Execução | `WaitClose` | Bloqueie novas entradas até que a posição líquida fique estável. |
| Execução | `MaxMarketOrders` | Máximo de lotes simultâneos por direção quando a pirâmide é permitida. |
| Pedidos pendentes | `EnableBuyStop` / `EnableSellStop` / `EnableBuyLimit` / `EnableSellLimit` | Ative ou desative cada tipo de pedido pendente. |
| Pedidos pendentes | `StopStepPoints`, `LimitStepPoints` | Distância em pontos usados para colocar ordens stop/limit em relação ao bid/ask atual. |
| Pedidos pendentes | `StopTakeProfitPoints`, `StopStopLossPoints`, `LimitTakeProfitPoints`, `LimitStopLossPoints` | Distâncias de proteção aplicadas quando as ordens pendentes são acionadas. |
| Pedidos pendentes | `StopTrailingOffsetPoints`, `StopTrailingStepPoints`, `LimitTrailingOffsetPoints`, `LimitTrailingStepPoints` | Parâmetros finais para ordens pendentes pendentes. |
| Risco de Mercado | `MarketTakeProfitPoints`, `MarketStopLossPoints` | Take-profit e stop-loss em pontos para posições de mercado. |
| Risco de Mercado | `MarketTrailingOffsetPoints`, `MarketTrailingStepPoints`, `RequireProfitBeforeTrailing` | Configuração de trailing stop para posições de mercado. |
| Risco de Mercado | `ExitProfitPoints` | Meta adicional de lucro fixo. |
| Gerenciamento de tempo | `UseTimeSignals`, `SignalHour`, `SignalMinute` | Configurações de execução agendada. |
| Gerenciamento de tempo | `TimeBuySignal`, `TimeSellSignal`, `TimeBuyStopSignal`, `TimeSellStopSignal`, `TimeBuyLimitSignal`, `TimeSellLimitSignal` | Quais ordens serão acionadas quando o cronômetro disparar. |
| Monitoramento de conta | `UseGlobalLevels`, `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Limites de alerta em nível de portfólio. |
| Diversos | `SlippagePoints` | Parâmetro legado reservado mantido para integridade. |

## Notas de conversão

- O especialista MQL definiu take-profit/stop-loss diretamente em pedidos pendentes. StockSharp coloca a entrada pendente primeiro e depois gerencia as saídas por meio da lógica estratégica para manter a implementação dentro das restrições API de alto nível.
- Os alertas sonoros foram omitidos porque o registro StockSharp já fornece notificações estruturadas.
- A restrição `MODE_STOPLEVEL` de MetaTrader não existe em StockSharp; portanto, os parâmetros dependem do trader para respeitar as distâncias mínimas impostas pela bolsa.
- O tratamento de erros usa `AddInfoLog`/`AddWarningLog` em vez de `Alert()` pop-ups.

## Uso

1. Anexe a estratégia a um `Security` e `Portfolio` com uma etapa de preço válida.
2. Configure distâncias em pontos (elas são convertidas automaticamente em unidades de preço usando o `ShrinkPrice` do título).
3. Inicie a estratégia; ele assinará cotações de nível 1 e começará a gerenciar pedidos imediatamente.

> **Dica:** Ao fazer backtesting, certifique-se de que o testador alimente dados de nível 1 para que a lógica de rastreamento e tempo receba atualizações em cada cotação, assim como o especialista original MQL.
