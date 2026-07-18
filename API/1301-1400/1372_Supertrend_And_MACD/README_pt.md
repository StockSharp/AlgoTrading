# Supertrend e MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina Supertrend, MACD e filtro EMA 200.

## Detalhes

- **Critérios de entrada**: Preço em relação ao Supertrend e EMA, linha MACD versus linha de sinal.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento do MACD ou stop baseado em extremos recentes.
- **Stops**: Stops de rastreamento de máximos/mínimos.
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, EMA, MACD, Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
