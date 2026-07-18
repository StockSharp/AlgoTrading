# Estratégia Dez Pontos 3 v005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista "10points 3 v005". Ele segue a inclinação MACD para decidir se a cesta média atual deve ser longa ou curta e continua abrindo ordens martingale sempre que o preço se move contra a posição ativa por uma distância configurável. A versão aprimorada "v005" adiciona regras de proteção baseadas em ações, filtros de dia e hora e a opção de desativar o ciclo longo ou curto.

## Lógica de negociação
- Leia a direção da linha principal MACD. Quando o indicador subir, a próxima cesta será comprada, quando cair, a cesta será curta. Uma opção permite reverter a interpretação.
- Abra a primeira posição de mercado imediatamente assim que existir uma direção. As entradas subsequentes são adicionadas sempre que o preço se move `EntryDistancePips` contra a posição flutuante.
- Os tamanhos dos pedidos crescem geometricamente. O multiplicador é controlado por `MartingaleFactor` (ou `HighTradeFactor` quando mais de 12 negociações são permitidas). Os volumes são alinhados à etapa de volume do instrumento e limitados a 100 lotes.
- Cada entrada atualiza os níveis agregados de stop-loss e take-profit. Os valores iniciais são compensados ​​por `InitialStopPips` e `TakeProfitPips`, enquanto a lógica final é ativada depois que a posição ganha `EntryDistancePips + TrailingStopPips` a favor.
- Se a proteção da conta estiver ativada, a estratégia pode alinhar a meta com a melhor entrada (`ReboundLock`) e fechar o pedido mais recente quando o lucro flutuante atingir `SecureProfit`.
- As regras de proteção de ações fecham todo o cabaz quando a perda flutuante excede `StopLossAmount`, quando o capital próprio sobe acima de `ProfitTarget + ProfitBuffer` ou quando o capital cai abaixo de `StartProtectionLevel`.
- A negociação é limitada à janela `OpenHour`/`CloseHour` e é completamente desativada às sextas-feiras por padrão.

## Gestão de dinheiro
Quando `UseMoneyManagement` está desabilitado, o primeiro pedido usa o `LotSize` fixo. Quando a sinalização está habilitada, o volume base é calculado a partir do valor atual do portfólio e do parâmetro `RiskPercent`. O escalonamento de minicontas pode ser simulado por meio de `IsStandardAccount`.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Distância (em pips) do take-profit aplicado a cada entrada. |
| `LotSize` | Tamanho base do lote quando o gerenciamento de dinheiro está desativado. |
| `InitialStopPips` | Distância inicial de stop loss para cada pedido. |
| `TrailingStopPips` | Distância de parada final quando o limite de disparo é atingido. |
| `MaxTrades` | Número máximo de entradas simultâneas de martingale. |
| `EntryDistancePips` | Movimento adverso mínimo necessário para adicionar o próximo pedido. |
| `SecureProfit` | Lucro flutuante (em unidades monetárias) necessário para acionar a saída de proteção de conta. |
| `UseAccountProtection` | Ativa a lógica de lucro seguro e bloqueio de recuperação. |
| `OrdersToProtect` | Número de etapas finais do martingale protegidas pela regra do lucro seguro. |
| `ReverseSignals` | Inverte a interpretação da inclinação MACD. |
| `UseMoneyManagement` | Permite dimensionamento baseado em equilíbrio. |
| `RiskPercent` | Percentagem de risco utilizada pela fórmula de gestão de dinheiro. |
| `IsStandardAccount` | Usa escala de lote padrão em vez de mini escala. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Valores de pip usados para converter lucro flutuante em moeda. |
| `CandleType` | Período de vela usado para geração de sinal. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Configuração MACD. |
| `EnableLong`, `EnableShort` | Ative ou desative a cesta longa/curta. |
| `OpenHour`, `CloseHour`, `MinuteToStop` | Configuração da janela de negociação. |
| `StopLossProtection`, `StopLossAmount` | Guarda stop-loss baseada em ações. |
| `ProfitTargetEnabled`, `ProfitTarget`, `ProfitBuffer` | Bloqueio de lucro baseado em ações. |
| `StartProtectionEnabled`, `StartProtectionLevel` | Guarda de piso patrimonial. |
| `ReboundLock` | Alinha as saídas com a melhor entrada quando a proteção está ativa. |
| `MartingaleFactor`, `HighTradeFactor` | Martingale multiplicadores. |
| `CloseOnFriday` | Desativa a negociação às sextas-feiras. |

## Notas
- A estratégia usa StockSharp API (`SubscribeCandles` + `BindEx`) de alto nível e não expõe buffers de indicadores brutos.
- Cada guarda de ações fecha a cesta ativa usando ordens de mercado para replicar o comportamento original EA.
- Sempre valide os valores dos parâmetros, o tamanho do pip e o valor do pip em relação às especificações do seu corretor antes de usar a estratégia em produção.
