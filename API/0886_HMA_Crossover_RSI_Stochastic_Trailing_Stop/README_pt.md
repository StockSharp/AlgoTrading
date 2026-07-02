# Estratégia HMA Crossover RSI Stochastic Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa o cruzamento de HMA rápida e lenta com filtros RSI e Stochastic suavizado. Abre comprado quando a HMA rápida cruza acima da lenta com RSI e Stochastic abaixo dos limites, e abre vendido na condição oposta. Um trailing stop gerencia as saídas.

## Detalhes

- **Critérios de entrada**: Cruzamento da HMA rápida acima da lenta com RSI e Stochastic abaixo dos limites.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Trailing stop ou sinal oposto.
- **Stops**: Porcentagem de rastreamento.
- **Valores padrão**:
  - `FastHmaLength` = 5
  - `SlowHmaLength` = 20
  - `RsiPeriod` = 14
  - `RsiBuyLevel` = 45
  - `RsiSellLevel` = 60
  - `StochLength` = 14
  - `StochSmooth` = 3
  - `StochBuyLevel` = 39
  - `StochSellLevel` = 63
  - `TrailingPercent` = 5
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: HMA, RSI, Stochastic
  - Stops: Trailing
  - Complexidade: Básico
  - Período: 1h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
