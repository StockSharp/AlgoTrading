# Estratégia de Scalping 15m EMA MACD RSI ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping que combina um filtro de tendência EMA de 50 períodos, o momentum do histograma MACD e os níveis do RSI. A gestão de risco utiliza stop loss e take profit baseados em ATR.

A estratégia compra quando o preço está acima da EMA, o histograma MACD é positivo e o RSI está entre 50 e o nível de sobrecompra. Posições vendidas ocorrem quando o preço está abaixo da EMA, o histograma é negativo e o RSI está entre o nível de sobrevenda e 50. Stops e alvos seguem o fechamento por múltiplos do ATR.

## Detalhes

- **Critérios de entrada**: Preço relativo à EMA, sinal do histograma MACD, nível do RSI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit baseados em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 50
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `AtrPeriod` = 14
  - `SlAtrMultiplier` = 1m
  - `TpAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: EMA, MACD, RSI, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
