# Estratégia de Efeito Noturno de Alta Volatilidade em Cripto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra em uma posição comprada durante noites de alta volatilidade e fecha antes da meia-noite. A volatilidade é medida pelo desvio padrão dos retornos logarítmicos ao longo de um período configurável e comparada com a mediana da volatilidade histórica.

## Detalhes

- **Critérios de entrada**:
  - `currentHour == EntryHour && highVolatility` quando `UseVolatilityFilter`
  - `currentHour == EntryHour` quando o filtro está desativado
- **Comprado/Vendido**: Comprado
- **Stops**: Nenhum
- **Valores padrão**:
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Baseado em tempo
  - Direção: Somente comprado
  - Indicadores: StandardDeviation, Median
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
