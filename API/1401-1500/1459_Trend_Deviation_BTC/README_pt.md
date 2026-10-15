# Desvio de Tendência BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina cruzamentos DMI com Bollinger Bands e confirmações de Momentum, MACD, SuperTrend e Aroon. A estratégia procura desvios de preço dentro de uma tendência e entra quando múltiplos sinais se alinham.

## Detalhes

- **Critérios de entrada**: +DI cruzando acima de -DI, preço abaixo da banda superior de Bollinger e qualquer confirmação de Momentum/MACD/SuperTrend/Aroon.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `DmiPeriod` = 15
  - `BbLength` = 13
  - `BbMultiplier` = 2.3
  - `MomentumLength` = 10
  - `AroonLength` = 5
  - `MacdFast` = 15
  - `MacdSlow` = 200
  - `MacdSignal` = 25
  - `AtrPeriod` = 200
  - `SuperTrendFactor` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: DMI, Bollinger Bands, Momentum, MACD, SuperTrend, Aroon
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
