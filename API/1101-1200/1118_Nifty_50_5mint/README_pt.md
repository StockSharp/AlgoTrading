# Estratégia Nifty 50 de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Nifty 50 de 5 Minutos** opera rompimentos no índice Nifty 50 usando confirmação de DEMA, VWAP e Bandas de Bollinger.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: fechamento acima da máxima anterior, acima da banda superior de Bollinger e DEMA acima do VWAP.
  - **Vendido**: fechamento abaixo da mínima anterior, abaixo da banda inferior de Bollinger e DEMA abaixo do VWAP.
- **Comprado/Vendido**: ambos.
- **Critérios de saída**: stop-loss.
- **Stops**: sim, pontos fixos.
- **Valores padrão**:
  - `DemaPeriod = 6`
  - `BollingerLength = 20`
  - `BollingerStdDev = 2`
  - `LookbackPeriod = 5`
  - `StopLossPoints = 25`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: DEMA, VWAP, Bollinger Bands
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
