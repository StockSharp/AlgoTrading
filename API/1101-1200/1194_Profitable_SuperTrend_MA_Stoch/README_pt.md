# Estratégia Lucrativa SuperTrend + MA + Stoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina SuperTrend, cruzamento de média móvel e o oscilador Stochastic.

Tem como objetivo capturar tendências identificadas pelo SuperTrend e confirmar entradas com cruzamento de EMA e níveis do Stochastic. Inclui alvos opcionais de take profit e stop loss.

## Detalhes

- **Critérios de entrada**: Tendência pelo SuperTrend, cruzamento de EMA, limiares do Stochastic.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento de EMA oposto ou TP/SL.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, EMA, Stochastic
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
