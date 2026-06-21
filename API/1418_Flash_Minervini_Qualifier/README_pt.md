# Estratégia Flash Qualificador Minervini
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina cruzamento de EMA, SuperTrend e RSI de momentum com análise de estágios de Minervini para qualificar as operações.

## Detalhes

- **Critérios de entrada**: EMA acima da linha de trailing, tendência SuperTrend e RSI de momentum acima do limiar com filtro de estágio Minervini
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: trailing oposto ou virada do SuperTrend
- **Stops**: Não
- **Valores padrão**:
  - `MomRsiLength` = 10
  - `MomRsiThreshold` = 60
  - `EmaLength` = 12
  - `EmaPercent` = 0.01
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, SuperTrend, RSI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
