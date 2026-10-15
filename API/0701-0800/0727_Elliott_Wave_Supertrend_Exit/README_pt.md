# Estratégia Elliott Wave com Saída por Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra em reversões similares a ZigZag e sai quando a direção do Supertrend muda, com um stop-loss fixo em percentual.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço forma uma mínima local
  - Vendido: o preço forma uma máxima local
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Mudança de direção do Supertrend ou nível de stop-loss
- **Stops**: Percentual fixo a partir do preço de entrada
- **Valores padrão**:
  - `WaveLength` = 4
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3
  - `StopLossPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest, SuperTrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
