# Estratégia Neon de Ondas de Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Neon de Ondas de Momentum usa cruzamentos do histograma MACD para operar em ambas as direções. A estratégia vai comprado quando o histograma cruza acima do nível de entrada (zero por padrão) e vai vendido quando cruza abaixo. As posições são fechadas quando o histograma atinge os níveis de saída configurados.

## Detalhes

- **Critérios de entrada**: O histograma MACD cruza o nível de entrada.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O histograma cruza os níveis de saída de comprado/vendido.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 20
  - `EntryLevel` = 0
  - `LongExitLevel` = 11
  - `ShortExitLevel` = -9
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
