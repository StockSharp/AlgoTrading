# HMA Crossover ATR Curvature
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

HMA Crossover ATR Curvature é uma estratégia de seguidor de tendência que combina um cruzamento de Hull Moving Average rápida e lenta com um filtro de curvatura. O tamanho da posição é baseado no ATR e no percentual de risco, e as operações são protegidas por um stop de rastreamento baseado em ATR.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: HMA rápida cruza acima da HMA lenta e a curvatura está acima do limite.
  - **Vendido**: HMA rápida cruza abaixo da HMA lenta e a curvatura está abaixo do limite negativo.
- **Critérios de saída**: Trailing stop ATR.
- **Stops**: Trailing stop ATR.
- **Valores padrão**:
  - `FastLength` = 15
  - `SlowLength` = 34
  - `AtrLength` = 14
  - `RiskPercent` = 1
  - `AtrMultiplier` = 1.5
  - `TrailMultiplier` = 1
  - `CurvatureThreshold` = 0
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado & Vendido
  - Indicadores: HMA, ATR
  - Complexidade: Baixo
  - Nível de risco: Médio
