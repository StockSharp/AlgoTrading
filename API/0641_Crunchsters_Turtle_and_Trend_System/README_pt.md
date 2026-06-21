# Estratégia do Sistema Turtle e Tendência de Crunchster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina um filtro de tendência EMA rápida/lenta com entradas por rompimento do canal Donchian e gerenciamento de stops baseado em ATR. Um canal Donchian em trailing encerra posições quando o momentum se reverte.

## Detalhes

- **Critérios de entrada**: Cruzamento diferencial de EMA ou rompimento do canal Donchian.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Canal trailing ou stop ATR.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `CandleType` = 1 hora
  - `FastEmaPeriod` = 10
  - `BreakoutPeriod` = 20
  - `TrailPeriod` = 1000
  - `StopAtrMultiple` = 20
  - `OrderPercent` = 10
  - `TrendEnabled` = true
  - `BreakoutEnabled` = false
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado e Vendido
  - Indicadores: EMA, Donchian, ATR
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
