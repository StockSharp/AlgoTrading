# Estratégia Eliora Gold 1m Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa velas Heikin Ashi em um período de um minuto. Entra em velas fortes alinhadas com a tendência quando o mercado não está em consolidação e aplica um período de resfriamento entre operações. As saídas são gerenciadas por um stop trailing baseado em ATR.

## Detalhes

- **Critérios de entrada**: vela Heikin Ashi forte na direção da tendência, sem consolidação, filtro de volatilidade.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: stop trailing baseado em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, ATR, SMA, Highest/Lowest
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
