# Estratégia PEAD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera o drift pós-anúncio de resultados após uma surpresa positiva de EPS e um gap de alta.
Entra comprado no dia seguinte ao resultado quando o preço abre com gap de alta e o desempenho recente é positivo,
utilizando trailing de EMA, stop fixo/breakeven e período máximo de manutenção.

## Detalhes

- **Critérios de entrada**: Surpresa positiva de EPS, gap de alta após resultados e desempenho prévio positivo.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Cruzamento abaixo da EMA diária, stop fixo/breakeven ou máximo de barras de manutenção.
- **Stops**: Stop fixo com breakeven.
- **Valores padrão**:
  - `GapThreshold` = 1
  - `EpsSurpriseThreshold` = 5
  - `PerfDays` = 20
  - `StopPct` = 8
  - `EmaLen` = 50
  - `MaxHoldBars` = 50
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Earnings
  - Direção: Long
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
