# Estratégia ZMFX Stolid 5a EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguimento de tendência multi-período que entra em correções confirmadas por leituras de RSI e Stochastic.
O sistema identifica a tendência principal a partir do Stochastic de 4 horas e médias móveis suavizadas de 1 hora.
As posições são abertas em reversões de candle com condições de RSI em sobrecompra/sobrevenda e fechadas em sinais opostos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `UpTrend && PreviousBarDown && PrevRSI < 30 && (RSI15 < 30 => double volume)`
  - Vendido: `DownTrend && PreviousBarUp && PrevRSI > 70 && (RSI15 > 70 => double volume)`
- **Comprado/Vendido**: Ambos
- **Stops**: Sem stops explícitos; posições fechadas por condições de indicadores
- **Valores padrão**:
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI, Stochastic, Smoothed Moving Average
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
