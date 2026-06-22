# Estratégia ASCTrendND
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é inspirada no consultor especialista ASCTrendND do MQL5. Usa uma Média Móvel Simples como principal sinal de tendência, um filtro RSI para confirmar a força e um stop trailing baseado em ATR para sair das negociações. A abordagem tenta replicar a lógica ASCTrend + NRTR + TrendStrength de forma simplificada na API de alto nível do StockSharp.

## Detalhes

- **Critérios de entrada:**
  - **Comprado:** O preço de fechamento está acima da SMA e RSI > 50.
  - **Vendido:** O preço de fechamento está abaixo da SMA e RSI < 50.
- **Critérios de saída:**
  - Stop trailing baseado em ATR * multiplicador ou sinal oposto.
- **Stops:** Apenas stop trailing baseado em ATR.
- **Valores padrão:**
  - `SmaPeriod` = 50
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `CandleType` = velas de 5 minutos
- **Filtros:**
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: SMA, RSI, ATR
  - Stops: Trailing
  - Complexidade: Baixo
  - Período: 5m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
