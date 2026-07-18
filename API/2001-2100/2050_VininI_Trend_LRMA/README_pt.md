# Estratégia VininI Trend LRMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia VininI Trend LRMA usa uma Média Móvel de Regressão Linear (LRMA) para rastrear a direção do mercado. A estratégia suporta dois modos de entrada:
- **Breakdown**: opera quando LRMA cruza os níveis superior ou inferior fixos.
- **Twist**: opera quando LRMA inverte sua direção.

## Detalhes

- **Critérios de entrada**: LRMA cruza os níveis (Breakdown) ou muda de direção (Twist)
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `CandleType` = TimeFrameCandle 4h
  - `Period` = 13
  - `UpLevel` = 10
  - `DnLevel` = -10
  - `Mode` = Breakdown
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: LinearRegression
  - Stops: Nenhum
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
