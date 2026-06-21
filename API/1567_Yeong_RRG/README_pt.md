# Yeong RRG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na força relativa normalizada e na razão de momentum (RRG).

A estratégia entra comprada quando tanto JDK RS quanto JDK RoC estão acima de 100 e sai quando ambos caem abaixo de 100.

## Detalhes

- **Critérios de entrada**: JDK RS e JDK RoC acima de 100.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: JDK RS e JDK RoC abaixo de 100.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Relative Strength
  - Direção: Long
  - Indicadores: SMA, ROC, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

