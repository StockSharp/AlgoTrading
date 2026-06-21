# Estratégia de Rompimento de Momentum de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina níveis de rompimento baseados em ATR com filtro de tendência EMA e momentum RSI para capturar movimentos fortes.

## Detalhes

- **Critérios de entrada**: o preço fecha acima/abaixo dos níveis de rompimento ATR com confirmação de EMA e RSI
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss baseado em ATR e take profit com razão risco-recompensa 1:2
- **Stops**: ATR
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `Lookback` = 20
  - `EmaPeriod` = 50
  - `RsiPeriod` = 14
  - `RsiLongThreshold` = 50
  - `RsiShortThreshold` = 50
  - `RiskReward` = 2
  - `AtrStopMultiplier` = 1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, EMA, RSI, Highest, Lowest
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
