# Estratégia de Mercados em Tendência Nifty Options com TSL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de Rompimento usando Bollinger Bands com filtros ADX e Supertrend. As entradas requerem um pico de volume. As posições são fechadas em cruzamentos de MACD, enfraquecimento do ADX ou um trailing stop baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço cruza acima da banda superior de Bollinger && ADX > limiar && pico de volume && preço acima de Supertrend
  - Vendido: preço cruza abaixo da banda inferior de Bollinger && ADX > limiar && pico de volume && preço abaixo de Supertrend
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento de MACD, queda do ADX ou trailing stop por ATR
- **Stops**: Trailing stop por ATR
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
