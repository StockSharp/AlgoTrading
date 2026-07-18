# Estratégia de Day Trading MACD RSI EMA BB ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia intradiária que combina cruzamento de sinal MACD, limites de RSI e direção de tendência EMA com um filtro de contração de Bandas de Bollinger. O gerenciamento de risco utiliza stop-loss baseado em ATR, trailing stop e take-profit de risco-recompensa.

## Detalhes

- **Critérios de entrada**: MACD cruzando o sinal na direção da tendência, RSI dentro dos limites e sem contração de BB.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop oposto ou alvo.
- **Stops**: Stop-loss baseado em ATR, trailing stop e take-profit de risco-recompensa.
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `EmaFast` = 9
  - `EmaSlow` = 21
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `TrailAtrMultiplier` = 1.5
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, RSI, EMA, Bollinger Bands, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
