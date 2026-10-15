# Estratégia de Reversão à Média Linear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Reversão à Média Linear usa o z-score do preço em relação a uma média móvel para negociar reversão à média com um stop loss fixo em pontos.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: z-score < -EntryThreshold.
  - **Vendido**: z-score > EntryThreshold.
- **Critérios de saída**: O z-score retorna em direção ao zero (z-score > -ExitThreshold para comprados, z-score < ExitThreshold para vendidos).
- **Stops**: Stop loss fixo em pontos.
- **Valores padrão**:
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado & Vendido
  - Indicadores: SMA, StandardDeviation
  - Stops: Sim
  - Complexidade: Baixo
  - Nível de risco: Médio
