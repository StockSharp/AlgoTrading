# Estratégia Tri-Monthly BTC Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Tri-Monthly BTC Swing opera com EMA200, cruzamento de MACD e filtro RSI.
A estratégia permite apenas uma operação a cada 90 dias.

## Detalhes

- **Critérios de entrada**: fechamento acima da EMA200, linha MACD acima do sinal, RSI acima do limiar e pelo menos 90 dias desde a última operação
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: linha MACD abaixo do sinal ou RSI abaixo do limiar
- **Stops**: Não
- **Valores padrão**:
  - `EmaLength` = 200
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiThreshold` = 50
  - `TradeInterval` = 90 dias
  - `CandleType` = 1 dia
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA, MACD, RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
