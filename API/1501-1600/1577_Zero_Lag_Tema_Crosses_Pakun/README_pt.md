# Estratégia de Cruzamentos Zero-Lag TEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de cruzamento de tripla EMA sem lag. As posições usam máximos e mínimos recentes para os stops e alvos baseados em relação risco-recompensa.

## Detalhes

- **Critérios de entrada**: TEMA rápida cruzando a TEMA lenta.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop no extremo recente ou alvo por proporção.
- **Stops**: Sim.
- **Valores padrão**:
  - `Lookback` = 20
  - `FastPeriod` = 69
  - `SlowPeriod` = 130
  - `RiskReward` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: TEMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
