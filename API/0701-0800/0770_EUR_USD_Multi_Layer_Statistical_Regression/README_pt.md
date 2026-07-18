# Estratégia de Regressão Estatística Multi-Camada EUR/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa múltiplas camadas de regressão linear para estimar a direção da tendência no EUR/USD. Calcula regressões curtas, médias e longas, valida-as por limiares de R² e inclinação, e opera na direção do conjunto ponderado.

## Detalhes

- **Critérios de entrada**:
  - Comprado: inclinação ponderada > 0 e confiabilidade > 0.5
  - Vendido: inclinação ponderada < 0 e confiabilidade > 0.5
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Reverter quando o sinal oposto aparecer
- **Stops**: Proteção por perda diária
- **Valores padrão**:
  - `ShortLength` = 20
  - `MediumLength` = 50
  - `LongLength` = 100
  - `MinRSquared` = 0.45m
  - `SlopeThreshold` = 0.00005m
  - `WeightShort` = 0.4m
  - `WeightMedium` = 0.35m
  - `WeightLong` = 0.25m
  - `PositionSizePct` = 50m
  - `MaxDailyLossPct` = 12m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Linear Regression
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Nível de risco: Médio
