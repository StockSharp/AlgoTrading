# Estratégia Sail System EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Sail System EA é um scalper com hedge que mantém exposição comprada/vendida simétrica enquanto verifica constantemente requisitos da corretora, como spread máximo, nível mínimo de stop e limites de sessão. O port StockSharp recria o comportamento original com a API `Strategy` de alto nível: o motor assina cotações level-1, abre ou rearma os dois lados do hedge e gerencia níveis virtuais de stop-loss/take-profit sem usar chamadas de conector de baixo nível.

A implementação mantém dois objetos internos `PositionState` (compra e venda). Para cada lado, a estratégia acompanha preço de entrada, volume restante, níveis virtuais de proteção e ordens pendentes. Isso espelha o expert MQL que mantinha contadores de tickets separados para ordens a mercado e pendentes.

## Lógica de negociação
1. **Filtro de sessão.** A negociação pode ser restrita a uma janela configurável. Quando a hora atual fica fora da sessão, a estratégia mantém, cancela ou fecha a exposição existente dependendo de `ManageExistingOrders`.
2. **Monitor de spread.** Atualizações bid/ask são coletadas por `SubscribeLevel1()`. A estratégia verifica o spread instantâneo ou uma média rolante (até 100 amostras) e compara o valor a `MaxSpread` mais a comissão configurada. Se o spread estiver alto demais, o sistema pode fechar posições abertas e a distância de entrada pode ser multiplicada por `MultiplierIncrease` para esperar condições mais calmas.
3. **Motor de entrada.** Quando a negociação é permitida, a estratégia abre os dois lados com ordens a mercado ou mantém ordens limit pareadas, dependendo de `UsePendingOrders`. O preço limit para novas ordens é derivado do melhor bid/ask atual mais `DistancePending` (em pips) e um multiplicador de segurança opcional.
4. **Proteção virtual.** Cada execução define níveis virtuais de stop-loss e take-profit opcional usando `OrdersStopLoss` / `OrdersTakeProfit`. Níveis virtuais são recalculados após `DelayModifyOrders` atualizações de cotação, mas apenas quando a melhora é maior que `StepModifyOrders`. O mecanismo reproduz ajustes graduais de stop da versão MQL sem chamar `OrderModify`.
5. **Tratamento de saída.** Quando o bid (para compras) ou ask (para vendas) atinge o stop virtual ou alvo, a estratégia envia a ordem a mercado oposta para fechar a posição. Saídas são rotuladas conforme o motivo (stop loss, take-profit, fim de sessão ou violação de spread) para que o log de operações corresponda ao expert advisor.
6. **Gestão de reentrada.** Se ordens pendentes se afastam do mercado por mais de `PipsReplaceOrders` multiplicado por `SafeMultiplier`, elas são canceladas e recriadas em preços novos. Isso substitui a lógica de realocação baseada em timer do script MQL.
7. **Dimensionamento de lote.** Usa-se um `ManualLotSize` fixo ou o volume é derivado do patrimônio da carteira e `RiskFactor`, imitando o cálculo de auto-lote do código original.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` / `ManualLotSize` | Volume base por ordem quando dimensionamento automático está desabilitado. |
| `AutoLotSize`, `RiskFactor` | Habilita dimensionamento de lote baseado em patrimônio. |
| `UseVirtualLevels` | Mantém a lógica de stop-loss/take-profit no lado da estratégia. |
| `OrdersStopLoss`, `OrdersTakeProfit`, `PutTakeProfit` | Distâncias de proteção em pips. |
| `DelayModifyOrders`, `StepModifyOrders` | Controlam quão rápido níveis virtuais são atualizados. |
| `PipsReplaceOrders`, `SafeMultiplier` | Forçam reentrada quando ordens pendentes estão longe demais do mercado. |
| `UsePendingOrders`, `DistancePending` | Alternam entre entradas limit e a mercado. |
| `UseTimeFilter`, `TimeStartTrade`, `TimeStopTrade`, `ManageExistingOrders` | Configuração da janela de negociação. |
| `MaxSpread`, `TypeOfSpreadUse`, `HighSpreadAction`, `MultiplierIncrease`, `CloseOnHighSpread` | Filtro de spread e reação. |
| `CommissionInPip`, `CountAvgSpread`, `TimesForAverage` | Controles de média de spread. |
| `AcceptStopLevel`, `Slippage`, `OrdersId` | Nível mínimo de stop da corretora, slippage de execução e equivalente ao magic number. |

Todos os parâmetros são expostos via `StrategyParam<T>` para estarem disponíveis na UI do Designer e compatíveis com execuções de otimização.

## Diferenças em relação ao MQL
- StockSharp usa um modelo de posição líquida; portanto, a estratégia cancela a ordem pendente oposta quando um lado é executado para evitar zerar a posição líquida. Isso ainda preserva o comportamento de hedge alternado do EA original.
- A flag `UseVirtualLevels` mantém gestão de stop-loss/alvo dentro da estratégia. O expert MQL dependia de objetos gráficos para visualização; este port registra cada atualização em vez de desenhar linhas.
- A média de spread é implementada como média incremental contínua, substituindo o acumulador baseado em array do MQL enquanto respeita o mesmo limite de período de média.

## Uso da API de alto nível
- `SubscribeLevel1().Bind(ProcessLevel1)` conduz todo o motor de decisão com base em atualizações do melhor bid/ask.
- Ordens de entrada e saída são criadas por helpers estilo `RegisterOrder`, `BuyMarket`, `SellMarket`, exatamente como recomendado nas diretrizes de conversão.
- `StartProtection()` é invocado uma vez durante `OnStarted`, seguindo a melhor prática do framework para ativar suporte a ordens protetoras.
