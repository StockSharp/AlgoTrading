# Estratégia SuperTrend Duplo com Filtro VIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina dois indicadores SuperTrend com um filtro de volatilidade baseado em VIX. Uma posição comprada é aberta quando ambos os SuperTrends são de alta e o índice VIX está acima de sua média. Uma posição vendida é aberta quando ambos os SuperTrends são de baixa e o VIX sobe acima de sua média mais uma margem de desvio padrão. As posições são encerradas quando qualquer um dos SuperTrends muda de direção.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Ambos os SuperTrends indicam tendência de alta e o VIX está acima de sua média.
  - **Vendido**: Ambos os SuperTrends indicam tendência de baixa e o VIX está acima de sua média e em alta.
- **Critérios de saída**:
  - Sinal oposto do SuperTrend.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `StLength1` = 13
  - `StMultiplier1` = 3.5
  - `StLength2` = 8
  - `StMultiplier2` = 5
  - `UseVixFilter` = true
  - `VixLookback` = 252
  - `VixTrendPeriod` = 10
  - `StdDevMultiplier` = 1
  - `EnableLong` = true
  - `EnableShort` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, SMA, StandardDeviation, EMA
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
