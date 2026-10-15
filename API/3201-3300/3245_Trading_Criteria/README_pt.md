# Estratégia de Trading Criteria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia de Trading Criteria é uma abordagem de seguimento de tendência multi-período convertida do consultor especialista original MQL4 "Trading Criteria". O port baseia-se em médias móveis lineares ponderadas, filtros de desvio de momentum e confirmações MACD extraídas de períodos de tendência e mensal. Os recursos de gerenciamento de risco incluem trailing stops, proteção de break-even e alvos configuráveis de stop-loss/take-profit.

## Lógica de entrada

1. **Período primário**: Usa uma média móvel linear ponderada (LWMA) rápida e lenta. Sinais comprados exigem que a MA rápida permaneça acima da lenta; vendidos exigem o contrário.
2. **Filtro de momentum**: Calcula o desvio do momentum (|Momentum-100|) no período de tendência e verifica os três valores mais recentes contra limiares altistas ou baixistas.
3. **Filtro MACD de tendência**: Avalia a linha principal do MACD relativa à sua linha de sinal no mesmo período de tendência. Os sinais só são disparados quando a relação atual se alinha com a barra anterior para evitar mudanças rápidas.
4. **Filtro MACD mensal**: Confirma o viés direcional maior usando MACD em um período mensal (ou especificado pelo usuário como lento).
5. **Exposição de posição**: Limita o tamanho máximo de posição líquida a `MaxPositions * Volume`. Se um novo sinal aparecer enquanto se mantém uma posição oposta, a estratégia primeiro neutralizará a exposição comprando ou vendendo volume suficiente.

## Saída e gerenciamento de risco

- **Stop Loss / Take Profit**: Definido via `StopLossPoints` e `TakeProfitPoints`, convertido em offsets de preço real usando o tamanho de pip normalizado do instrumento.
- **Trailing stop**: Habilitado com `EnableTrailing` e `TrailingStopPoints`. Para comprados, o stop rastreia o preço mais alto menos a distância de trailing quando o movimento excede o limiar; vendidos espelham a lógica usando o preço mais baixo.
- **Movimento de break-even**: Quando habilitado (`EnableBreakEven`), o stop migra para o preço de entrada mais um offset opcional assim que o preço de fechamento atinge a distância `BreakEvenTriggerPoints` a favor da posição aberta.
- **Saídas protetoras manuais**: Se a vela tocar os níveis calculados de stop ou alvo, a estratégia fecha toda a posição líquida naquela barra.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período base para geração de sinais e médias móveis. |
| `TrendCandleType` | Período usado para filtros de momentum e MACD. |
| `MonthlyCandleType` | Período lento que fornece confirmação MACD de longo prazo. |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimentos da LWMA rápida e lenta no período de entrada. |
| `MomentumPeriod` | Período de lookback do momentum no período de tendência. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desvio mínimo de 100 requerido para entradas compradas ou vendidas. |
| `MaxPositions` | Número máximo de lotes base que podem permanecer abertos simultaneamente. |
| `StopLossPoints` / `TakeProfitPoints` | Distâncias, em pontos, para stops protetores e alvos de lucro. |
| `EnableTrailing` / `TrailingStopPoints` | Ativa trailing stops e define sua distância. |
| `EnableBreakEven` | Ativa o comportamento de break-even. |
| `BreakEvenTriggerPoints` / `BreakEvenOffsetPoints` | Controla o quanto o preço deve se mover antes de o stop migrar para break-even e qual offset aplicar. |

## Notas de uso

- Anexar a estratégia a um instrumento com suporte adequado de séries de velas para os períodos selecionados.
- Garantir que o ativo forneça um `PriceStep` preciso; a implementação ajusta instrumentos de pip fracionário (3 ou 5 casas decimais) para corresponder às convenções MQL.
- As proteções de trailing e break-even operam em velas concluídas. Em mercados rápidos, os níveis protetores podem ser executados na barra seguinte quando ocorre um gap.
- O conjunto de parâmetros padrão reflete os inputs MQL publicados, mas podem ser otimizados via os metadados de parâmetros integrados.
