# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

PresentTrend RMI Synergy combina um filtro de momentum baseado em RSI com um trailing stop ATR no estilo SuperTrend. As entradas ocorrem quando o momentum supera os limites e o preço está alinhado com a tendência. O stop segue dinamicamente o preço usando uma média móvel e uma banda ATR.

Os backtests mostram desempenho estável em mercados de tendência como cripto.

## Detalhes

- **Critérios de entrada**: RMI acima de 60 com preço acima da média móvel para comprados; RMI abaixo de 40 com preço abaixo da média móvel para vendidos.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Trailing stop baseado em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `RmiPeriod` = 21
  - `SuperTrendLength` = 5
  - `SuperTrendMultiplier` = 4.0m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI, ATR, SMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
