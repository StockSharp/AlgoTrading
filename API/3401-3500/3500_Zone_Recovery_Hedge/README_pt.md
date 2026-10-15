# Estratégia de Hedge de Recuperação de Zona
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Hedge de Recuperação de Zona** é uma versão StockSharp do consultor especialista MetaTrader *Zone Recovery Hedge V1*. O algoritmo alterna posições de compra e venda em torno de um preço âncora para que uma nova ordem seja colocada sempre que o preço cruzar a zona de recuperação configurada. A sequência expande o volume da posição seguindo um cronograma martingale até que a meta de lucro flutuante ou a proteção opcional contra perdas seja alcançada.

## Lógica estratégica

1. **Filtro de entrada** – Quando o modo *RSI Multi-Timeframe* é selecionado, a estratégia inspeciona uma lista configurável de RSI leituras (de M1 a MN1) e exige que cada período de tempo ativado deixe uma área de sobrecompra/sobrevenda simultaneamente. Sair da sobrevenda gera um ciclo de compra, enquanto passar da sobrecompra inicia um ciclo de venda. No modo *Manual*, os métodos auxiliares `StartManualMarketCycle` e `StartManualPendingCycle` podem ser chamados para iniciar uma nova sequência sem sinais automáticos.
2. **Negociação inicial** – A primeira negociação utiliza o tamanho de lote fixo ou um tamanho baseado no risco derivado do patrimônio do portfólio e da distância de parada planejada. Quando o dimensionamento ATR está ativo, a distância de parada e a largura da zona são derivadas do ATR diário; caso contrário, serão usados ​​pontos de corretor.
3. **Grade de recuperação** – Se o preço viajar contra a direção ativa pela distância da zona de recuperação, a estratégia abre o lado oposto com um volume aumentado (escada de lote personalizada, multiplicador ou etapa aditiva). O ciclo continua alternando direções em torno do preço âncora original, aumentando o volume até que a meta de lucro seja atingida ou o número máximo de negociações seja alcançado.
4. **Controle de lucro** – A meta é avaliada na moeda da conta, usando a distância base de lucro ou o lucro de recuperação dedicado (com frações opcionais de ATR). As comissões podem ser simuladas através do parâmetro *Comissão de Teste*. Quando o lucro flutuante excede a meta mais os custos, todo o ciclo é fechado.
5. **Proteção de risco** – Se `MaxTrades` for diferente de zero e `SetMaxLoss` estiver ativado, atingir a contagem máxima de negociação enquanto o PnL flutuante viola o limite `MaxLoss` fechará todas as posições e reiniciará o ciclo.

> **Observação:** estratégias StockSharp são compensadas por padrão. O porto reproduz a lógica de recuperação revertendo a posição líquida em vez de manter posições cobertas simultâneas. Isso mantém a matemática do lucro compatível com StockSharp enquanto preserva as etapas de recuperação alternadas do consultor original.

## Parâmetros

| Grupo | Parâmetro | Descrição |
| --- | --- | --- |
| Geral | `CandleType` | Período primário que orienta a lógica de entrada. |
| Geral | `Mode` | `Manual` desativa sinais, `RsiMultiTimeframe` ativa o filtro RSI. |
| Sinais | `RsiPeriod`, `OverboughtLevel`, `OversoldLevel` | RSI período de cálculo e limites. |
| Sinais | `UseM1Timeframe` … `UseMonthlyTimeframe` | Habilite/desabilite as confirmações RSI para o período correspondente. |
| Sinais | `TradeOnBarOpen` | Use a barra anterior como barra de confirmação (comportamento EA original). |
| Recuperação | `RecoveryZoneSize`, `TakeProfitPoints` | Largura da zona e lucro base quando ATR está desativado. |
| Recuperação | `UseAtr`, `AtrPeriod`, `AtrZoneFraction`, `AtrTakeProfitFraction`, `AtrRecoveryFraction`, `AtrCandleType` | Configurações de dimensionamento baseadas em ATR. |
| Recuperação | `UseRecoveryTakeProfit`, `RecoveryTakeProfitPoints` | Distância de take-profit dedicada quando o ciclo já está em recuperação. |
| Risco | `MaxTrades`, `SetMaxLoss`, `MaxLoss` | Limite o número de negociações e defina uma proteção contra perdas baseada em dinheiro. |
| Risco | `TestCommission` | Comissão estimada (em dinheiro) aplicada por volume de negociação ao avaliar a meta de lucro. |
| Gestão de dinheiro | `RiskPercent`, `InitialLotSize`, `LotMultiplier`, `LotAddition`, `CustomLotSize1` … `CustomLotSize10` | Controla como os volumes são gerados para cada etapa do ciclo. |
| Temporizador | `UseTimer`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute`, `UseLocalTime` | Restrinja a negociação a uma janela de tempo diária. |
| Manuais | `PendingPrice` | Preço de referência usado por `StartManualPendingCycle`. |

## Dicas de uso

- Anexe a estratégia a uma fonte de dados que forneça o prazo mais alto que você deseja usar para confirmações de RSI. Prazos mais altos podem ser construídos a partir do prazo base pelo agregador interno.
- Quando o modo *Manual* estiver ativo, chame `StartManualMarketCycle(true)` ou `StartManualMarketCycle(false)` para abrir um ciclo de compra ou venda no preço atual, ou `StartManualPendingCycle` para ancorar o ciclo em um nível de preço personalizado.
- O dimensionamento da posição com base no equilíbrio limita a porcentagem de risco em 10%, assim como o EA original.
- A lógica de recuperação assume que `Security.PriceStep` e `Security.StepPrice` são preenchidos pelo conector. Sem eles a meta de lucro não pode ser calculada.

## Diferenças da versão MetaTrader

- A porta StockSharp funciona com posições líquidas em vez de cestas longas/curtas cobertas. A sequência de recuperação ainda alterna as direções de negociação, mas as posições são invertidas ao mudar de direção.
- Os elementos gráficos (botões, linhas, comentários) do painel MT4 não são reproduzidos. Os comandos manuais e de temporizador são expostos por meio de parâmetros de estratégia e métodos auxiliares.
- A modelagem de custos baseada em spread é omitida; apenas o valor `TestCommission` é subtraído da meta de lucro.
