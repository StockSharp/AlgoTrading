# Estratégia Dual Supertrend MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Dual Supertrend MACD combina dois indicadores Supertrend com um filtro MACD.
Uma posição comprada é aberta quando o preço opera acima de ambas as linhas Supertrend e o histograma MACD é positivo.
Posições vendidas surgem quando o preço está abaixo de ambas as linhas e o histograma é negativo.
As posições são fechadas assim que qualquer Supertrend inverte a direção ou o histograma MACD cruza o zero.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - Comprado: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - Vendido: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **Critérios de saída**:
  - Comprado: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - Vendido: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Configurável
  - Indicadores: Supertrend, MACD
  - Complexidade: Intermediário
  - Nível de risco: Médio
