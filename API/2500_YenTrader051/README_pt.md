# Estratégia YenTrader051 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia YenTrader051 replica o consultor especializado original do MetaTrader que arbitra a relação entre três pares de moedas:

- **Par cruzado negociado** – o instrumento que hospeda a instância da estratégia (por exemplo GBPJPY).
- **Par principal** – tipicamente a moeda base do cruzado contra USD (por exemplo GBPUSD).
- **USDJPY** – usado para confirmar a perna do iene do triângulo.

Um rompimento no par principal combinado com confirmação do USDJPY gera os sinais de trading. Filtros opcionais de RSI, CCI, RVI e média móvel refinam as entradas. O gerenciamento de posições suporta tanto o averaging quanto o pyramiding, enquanto o gerenciamento de risco reproduz o tratamento de stops baseado em pip/ATR do EA.

## Lógica de trading

1. **Detecção de rompimento**
   - `LoopBackBars` controla a janela de lookback. Quando maior que 1, a estratégia verifica:
     - máximos/mínimos recentes (`PriceReference = HighLow`), ou
     - fechamentos de `LoopBackBars` barras atrás (`PriceReference = Close`).
   - `MajorDirection` define como o par principal e a perna do iene devem se mover em relação um ao outro quando o cruzado é cotado como principal/iene (Left) ou iene/principal (Right).
2. **Filtros de entrada**
   - `UseRsiFilter` requer RSI acima/abaixo de 50 dependendo do alinhamento de tendência esperado.
   - `UseCciFilter` força o CCI a ser positivo/negativo.
   - `UseRviFilter` aguarda o RVI cruzar sua linha de sinal. A linha de sinal é uma SMA de 4 períodos dos valores do RVI, assim como na implementação do MT4.
   - `UseMovingAverageFilter` mantém as entradas alinhadas com uma média móvel configurável (`MaMode`, `MaPeriod`).
3. **Estilo de entrada**
   - `EntryMode = Both` permite qualquer rompimento.
   - `EntryMode = Pyramiding` só adiciona em velas altistas/baixistas na direção da operação.
   - `EntryMode = Averaging` só adiciona quando a vela anterior fechou contra a posição para fazer average.
4. **Dimensionamento de ordens**
   - `FixedLotSize` coloca um volume constante.
   - Quando o lote fixo é zero, a estratégia usa `BalancePercentLotSize` e o valor atual do portfólio para dimensionar as operações.
   - `MaxOpenPositions` limita o tamanho cumulativo (número de entradas aditivas).
5. **Gerenciamento de risco**
   - Distâncias em pips (`StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips`) são traduzidas via `Security.MinPriceStep`.
   - Quando `EnableAtrLevels` está ativo, as distâncias ATR substituem os pips usando o ATR diário (`AtrCandleType`, `AtrPeriod`) e os multiplicadores respectivos.
   - Stops, take-profits, break-even, bloqueio de lucros e níveis de trailing são atualizados a partir de velas completadas, assim como na implementação MQL.
   - `CloseOnOpposite` fechará as posições existentes em vez de empilhar novas quando um rompimento oposto aparecer.
   - `AllowHedging` permite à estratégia adicionar a uma posição mesmo se uma posição contrária ainda estiver aberta. Note que as estratégias StockSharp usam posições líquidas, portanto posições simultâneas comprada/vendida não são suportadas; o flag controla efetivamente se a estratégia pode aumentar a exposição quando a posição líquida atual aponta na outra direção.

## Parâmetros

| Grupo | Nome | Descrição |
|-------|------|-----------|
| Instrumentos | `MajorSecurity` | Par principal usado para confirmação de rompimento. |
| | `UsdJpySecurity` | Instrumento USDJPY para confirmação da perna do iene. |
| Dados | `CandleType` | Período de sinal para os três pares. |
| Filtros | `MajorDirection` | Alinhamento entre o par principal e o cruzado negociado (Left = principal/iene, Right = iene/principal). |
| | `PriceReference` | Rompimento de máximo/mínimo ou comparação de fechamento diferido. |
| | `LoopBackBars` | Número de barras históricas para avaliar o rompimento. |
| | `EntryMode` | Averaging, pyramiding ou ambos. |
| Indicadores | `UseRsiFilter`, `UseCciFilter`, `UseRviFilter`, `UseMovingAverageFilter` | Ativar/desativar filtros de confirmação adicionais. |
| | `MaPeriod`, `MaMode` | Configuração de média móvel. |
| Risco | `FixedLotSize`, `BalancePercentLotSize` | Controles de volume. |
| | `MaxOpenPositions` | Número máximo de entradas aditivas. |
| | `StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips` | Distâncias de risco baseadas em pips. |
| | `EnableAtrLevels`, `AtrCandleType`, `AtrPeriod`, `AtrStopLossMultiplier`, `AtrTakeProfitMultiplier`, `AtrTrailingMultiplier`, `AtrBreakEvenMultiplier`, `AtrProfitLockMultiplier` | Configuração de risco baseada em ATR. |
| Comportamento | `CloseOnOpposite` | Fechar ou inverter posições em sinais opostos. |
| | `AllowHedging` | Permitir entradas quando existe uma posição líquida contrária. |

## Notas de uso

- Atribua o instrumento do cruzado negociado à propriedade `Security` da estratégia, depois defina `MajorSecurity` e `UsdJpySecurity` para os instrumentos de suporte.
- Garanta que o portfólio esteja conectado; o dimensionamento de lotes variáveis requer `Portfolio.CurrentValue`.
- A estratégia espera dados de velas sincronizados para os três instrumentos. Se diferentes bolsas entregam dados com calendários de sessão diferentes, considere resamplear para um período comum.
- Os cálculos de ATR assinam o `AtrCandleType` configurado. Mantenha alinhado com os padrões originais do EA (diário, 21 períodos) para comportamento comparável.
- A lógica de risco opera em velas fechadas, portanto as ordens de proteção são executadas por saídas de mercado quando os limiares são violados durante a vela subsequente.

## Diferenças vs. versão MT4

- StockSharp usa posições líquidas agregadas; o verdadeiro hedging (manter comprado e vendido simultaneamente) não está disponível. `AllowHedging` simplesmente controla se a estratégia pode inverter posições automaticamente quando um novo sinal aparece.
- O gerenciamento de stop/limite é implementado com saídas de mercado após os limiares serem acionados nos dados de velas. O EA original modifica os stops de ordens diretamente porque opera em nível de tick.
- A linha de sinal do RVI é implementada como uma SMA de quatro períodos dos valores do RVI, correspondendo ao comportamento de `MODE_SIGNAL` no MT4.
