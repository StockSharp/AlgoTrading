# Seguidor de tendência Zero-Lag MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de seguimento de tendência que aguarda o cruzamento de uma MA sem lag com uma EMA e então entra quando o preço rompe uma caixa de tamanho ATR. As posições incluem alvos baseados em relação risco-recompensa.

## Detalhes

- **Critérios de entrada**: Cruzamento de MA sem lag e rompimento da caixa.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop ou take profit baseados em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `Length` = 34
  - `AtrPeriod` = 14
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ZLEMA, EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
