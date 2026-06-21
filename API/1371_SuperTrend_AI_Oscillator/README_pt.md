# Estratégia SuperTrend AI com Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

SuperTrend AI Oscillator combina um stop trailing do SuperTrend com um filtro de oscilador personalizado.
A estratégia opera nas reversões do SuperTrend confirmadas pelo oscilador.
As posições são gerenciadas por um stop trailing e um alvo opcional de relação risco-recompensa.

## Detalhes

- **Critérios de entrada**: Inversão do SuperTrend com oscilador > 50 para comprado ou < 50 para vendido
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop trailing ou take profit de relação risco-recompensa
- **Stops**: Trailing
- **Valores padrão**:
  - `AtrLength` = 10
  - `Factor` = 1
  - `RiskReward` = 2
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Stochastic
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
