# MACD Estratégia PSAR corrigida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão C# do consultor especialista MetaTrader **EA_MACD_FixedPSAR**. Ele negocia reversões de tendência combinando um filtro cruzado MACD com uma verificação de tendência EMA. O gerenciamento de riscos reflete a implementação original e suporta um trailing stop de distância fixa e um modo de trailing estilo Parabolic SAR. Todas as distâncias são configuradas em pips e convertidas internamente em unidades de preço com base no tamanho do tick do instrumento.

## Indicadores
- `MovingAverageConvergenceDivergenceSignal` (12, 26, 9) fornece MACD e linhas de sinal.
- `ExponentialMovingAverage` (padrão 26) confirma a direção da tendência de curto prazo.

## Lógica de negociação
1. **Condições de entrada**
   - **Longo**: MACD cruza acima de sua linha de sinal enquanto permanece abaixo de zero, o valor absoluto de MACD excede o *MACD nível aberto* e o EMA está subindo em comparação com a vela anterior.
   - **Venda**: MACD cruza abaixo de sua linha de sinal enquanto permanece acima de zero, o valor absoluto de MACD excede o *MACD nível aberto* e o EMA está caindo em comparação com a vela anterior.
2. **Exit Conditions**
   - MACD reversão que excede o *MACD Nível de Fechamento* na direção oposta.
   - Níveis configuráveis de take-profit e stop-loss, ambos medidos em pips.
   - Comportamento de parada móvel opcional:
     - **Fixo**: mantém uma distância constante do último fechamento.
     - **Corrigido PSAR**: emula o ajuste incremental Parabolic SAR usado pela versão MQL.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `Volume` | Volume de negociação utilizado para ordens de mercado. |
| `TakeProfitPips` | Distância de lucro em pips. |
| `StopLossPips` | Distância de stop-loss em pips. |
| `TrailMode` | Lógica de parada final (`None`, `Fixed`, `FixedPsar`). |
| `TrailingStopPips` | Distância para o modo de rastreamento fixo. |
| `PsarStep` | Fator de aceleração inicial para o modo de trilha PSAR. |
| `PsarMaximum` | Fator máximo de aceleração para o modo de trilha PSAR. |
| `MacdOpenLevelPips` | Magnitude mínima de MACD (em pips) necessária para abrir uma posição. |
| `MacdCloseLevelPips` | Magnitude mínima de MACD (em pips) necessária para fechar uma posição. |
| `TrendPeriod` | Período EMA usado para confirmação de tendência. |
| `CandleType` | Tipo de série de velas para cálculos de indicadores. |

## Notas
- Todos os limites são armazenados em pips e traduzidos em unidades de preço usando o tamanho do tick do instrumento (com cinco ou três correções decimais emulando o ajuste MetaTrader).
- A lógica de trailing stop é atualizada apenas em velas totalmente formadas para evitar saídas prematuras.
- A estratégia desenha velas, indicadores e marcas registradas na área padrão do gráfico, quando disponível.
