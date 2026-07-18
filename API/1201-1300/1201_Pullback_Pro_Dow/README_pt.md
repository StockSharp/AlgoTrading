# Estratégia Pullback Pro Dow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza pivôs da Teoria Dow para definir a direção da tendência e entra em retrocessos de EMA quando a força da tendência é confirmada pelo ADX. O sistema realiza saídas escalonadas em dois alvos de risco-recompensa.

Backtests mostram comportamento estável em futuros de índices como o US30.

## Detalhes

- **Critérios de entrada**:
  - Comprado: topos e fundos ascendentes, mínima cruza abaixo da EMA, ADX acima do limiar
  - Vendido: topos e fundos descendentes, máxima cruza acima da EMA, ADX acima do limiar
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop no último pivô, realização de lucro em dois alvos R:R
- **Stops**: Baseado em pivôs
- **Valores padrão**:
  - `PivotLookback` = 10
  - `EmaLength` = 21
  - `RiskReward1` = 1.5m
  - `Tp1Percent` = 50
  - `RiskReward2` = 3m
  - `UseAdxFilter` = true
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, Average Directional Index
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
