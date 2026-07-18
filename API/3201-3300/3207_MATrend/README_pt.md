# 3207 – Estratégia de MA Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de MA Trend** replica o especialista MetaTrader *MA Trend.mq5* usando a API de alto nível do StockSharp. O bot segue uma única média móvel ponderada linear com um deslocamento configurável para frente. Quando o preço de fechamento sobe acima da média deslocada, a estratégia vai comprada, enquanto uma queda abaixo da média abre posições vendidas. Regras opcionais de stop-loss, take-profit e trailing stop replicam os controles de risco da implementação MQL original.

## Lógica de trading
1. Subscrever ao tipo de vela configurado (padrão: período de 1 minuto) e calcular uma média móvel usando o método selecionado e a fonte de preço.
2. Deslocar o valor da média móvel para frente pelo número solicitado de velas concluídas antes de compará-lo com o fechamento mais recente.
3. Gerar sinais:
   - **Comprado** – preço de fechamento acima da MA deslocada (invertido quando `ReverseSignals` está habilitado).
   - **Vendido** – preço de fechamento abaixo da MA deslocada (invertido quando `ReverseSignals` está habilitado).
4. Aplicar opções de gerenciamento de posição:
   - Fechar a exposição oposta antes de abrir uma negociação quando `CloseOpposite` é `true`.
   - Bloquear novas entradas se `OnlyOnePosition` estiver habilitado e já existir uma posição.
5. Gerenciar saídas com distâncias de stop-loss, take-profit e trailing stop expressas em pips. A lógica de trailing requer que o preço se mova por `TrailingStopPips + TrailingStepPips` antes de ajustar o stop, assim como o especialista MQL.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Tamanho da ordem em lotes/contratos. |
| `StopLossPips` | `int` | `50` | Distância do stop-loss em pips. Zero desabilita o stop fixo. |
| `TakeProfitPips` | `int` | `140` | Distância do take-profit em pips. Zero desabilita o alvo. |
| `TrailingStopPips` | `int` | `15` | Distância do trailing stop. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | `int` | `5` | Pips adicionais necessários antes de mover o trailing stop. Deve permanecer positivo quando `TrailingStopPips` for maior que zero. |
| `MaPeriod` | `int` | `12` | Comprimento da média móvel. |
| `MaShift` | `int` | `3` | Número de barras concluídas usadas para deslocar a média móvel para frente. |
| `MaMethod` | `MovingAverageKind` | `Weighted` | Modo de cálculo da média móvel (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceMode` | `Weighted` | Preço de vela usado como entrada do indicador (Close, Open, High, Low, Median, Typical, Weighted). |
| `OnlyOnePosition` | `bool` | `false` | Restringir a estratégia a uma única posição aberta. |
| `ReverseSignals` | `bool` | `false` | Trocar as direções de sinal comprado/vendido. |
| `CloseOpposite` | `bool` | `false` | Fechar a exposição oposta antes de entrar em uma nova posição. |
| `CandleType` | `DataType` | `1 minute` | Tipo de vela/período fornecido ao indicador. |

## Notas
- O tamanho do pip se adapta automaticamente a instrumentos com preços de 3/5 decimais para coincidir com o comportamento original do MetaTrader.
- A validação do trailing stop reproduz a verificação MQL: se `TrailingStopPips > 0` e `TrailingStepPips <= 0`, a estratégia lança uma exceção durante o início.
- Todas as atualizações de indicadores e decisões de ordens usam apenas velas concluídas, garantindo backtests deterministas.
