# Estratégia Heatmap MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema usa um mapa de calor de histogramas MACD de cinco períodos. Quando todos os histogramas mudam acima ou abaixo de zero, entra na direção correspondente e sai quando o alinhamento é quebrado ou os limites de risco são acionados.

## Detalhes

- **Critérios de entrada**: Todos os histogramas MACD acima/abaixo de zero.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O alinhamento dos histogramas é quebrado ou os stops são acionados.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastPeriod` = 20
  - `SlowPeriod` = 50
  - `SignalPeriod` = 50
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `CandleType1` = TimeSpan.FromMinutes(60)
  - `CandleType2` = TimeSpan.FromMinutes(120)
  - `CandleType3` = TimeSpan.FromMinutes(240)
  - `CandleType4` = TimeSpan.FromMinutes(240)
  - `CandleType5` = TimeSpan.FromMinutes(480)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
