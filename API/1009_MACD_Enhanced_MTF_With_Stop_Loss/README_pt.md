# Estratégia MACD Aprimorada MTF com Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia multi-período que usa pontuação baseada em MACD e uma linha de trailing stop derivada do ATR.

## Detalhes

- **Critérios de entrada**: A pontuação MACD se torna positiva ou negativa.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou rompimento da linha de trailing stop.
- **Stops**: Trailing stop ATR.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CrossScore` = 10
  - `IndicatorScore` = 8
  - `HistogramScore` = 2
  - `StopLossFactor` = 1.2
  - `StopLossPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
