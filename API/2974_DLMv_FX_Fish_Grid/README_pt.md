# Estratégia DLMv FX Fish Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia DLMv FX Fish Grid** replica o comportamento do consultor especialista original do MetaTrader construído em torno do oscilador "FX Fish 2MA". A estratégia avalia a Transformada de Fisher do preço, suaviza-a com uma média móvel e abre posições quando o oscilador cruza sua linha de base suavizada no lado apropriado de zero. O gerenciamento de posições imita o comportamento em grade do EA de origem: as entradas adicionais são espaçadas por uma distância configurável, as ordens limite pendentes podem ser estratificadas e a automação protetora gerencia os controles de risco.

## Lógica de trading

1. **Cálculo do indicador**
   - Os preços mais altos e mais baixos durante as velas `CalculatePeriod` definem o intervalo rolante.
   - Uma Transformada de Fisher é aplicada ao preço selecionado (`AppliedPrice`), usando o mesmo fator de suavização 0.67 que o indicador MT5.
   - Uma média móvel simples (`MaPeriod`) do valor de Fisher fornece a linha de base do sinal.
2. **Geração de sinais**
   - **Sinal comprado**: os valores atual e anterior de Fisher estão abaixo de zero enquanto o oscilador cruza **acima** de sua média móvel (valor anterior abaixo da média, valor atual acima).
   - **Sinal vendido**: os valores atual e anterior de Fisher estão acima de zero enquanto o oscilador cruza **abaixo** da média móvel (valor anterior acima da média, valor atual abaixo).
   - Os sinais podem ser invertidos habilitando `ReverseSignals`.
3. **Execução de ordens**
   - Quando um sinal de compra (ou venda) aparece, a estratégia pode opcionalmente fechar a exposição oposta existente (`CloseOpposite`).
   - Entradas adicionais são permitidas até que a contagem total atinja `MaxTrades`. Cada nova entrada deve respeitar o espaçamento mínimo dado por `DistancePips` a partir da última operação preenchida.
   - Ordens limite opcionais (`SetLimitOrders`) colocam ofertas/demandas em repouso no espaçamento configurado, replicando a grade escalonada do EA original.
4. **Gestão de riscos**
   - Valores fixos de stop-loss, take-profit e trailing stop são aplicados via `StartProtection`, todos definidos em pips.
   - `TimeLiveSeconds` fecha toda a exposição quando uma operação ficou aberta mais do que o tempo de vida permitido.
   - O trading pode ser desabilitado às sextas-feiras (`TradeOnFriday = false`). Quando desabilitado, a estratégia fecha posições e cancela ordens pendentes assim que uma vela de sexta-feira chega.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Tamanho da ordem para cada entrada (lotes). |
| `StopLossPips` | Distância do stop-loss protetor a partir da entrada. Definir como 0 para desabilitar. |
| `TakeProfitPips` | Distância do nível de take-profit. Definir como 0 para desabilitar. |
| `TrailingStopPips` | Distância do trailing stop (0 desabilita o trailing). |
| `TrailingStepPips` | Passo pelo qual o trailing stop é ajustado. |
| `MaxTrades` | Número máximo de operações simultâneas por direção. `0` remove o limite. |
| `DistancePips` | Distância mínima entre entradas consecutivas e para as ordens de grade opcionais. |
| `TradeOnFriday` | Quando `false`, a estratégia para de operar às sextas-feiras e liquida a exposição. |
| `TimeLiveSeconds` | Tempo máximo (segundos) que as posições podem permanecer abertas antes de serem fechadas forçosamente. |
| `ReverseSignals` | Inverter condições comprado/vendido. |
| `SetLimitOrders` | Habilitar ordens limite adicionais em repouso em `DistancePips`. |
| `CloseOpposite` | Fechar a exposição oposta antes de entrar em uma nova operação. |
| `CalculatePeriod` | Lookback para o intervalo da Transformada de Fisher. |
| `MaPeriod` | Período da média móvel aplicada ao valor de Fisher. |
| `AppliedPrice` | Fonte de preço usada na Transformada de Fisher (close, open, high, low, median, typical, weighted). |
| `CandleType` | Tipo de dados/período das velas processadas pela estratégia. |

## Notas

- As distâncias de stop-loss, take-profit e trailing stop são convertidas de pips para offsets de preço absolutos usando `Security.PriceStep * 10`, correspondendo à lógica de pip de cinco dígitos da versão MQL.
- As ordens limite são canceladas automaticamente quando os sinais mudam, o trading é pausado ou as proteções de tempo/sexta-feira são acionadas.
- A Transformada de Fisher evita buscas repetidas de valores, armazenando as leituras anteriores do oscilador e da linha de base para detecção precisa de cruzamentos.
