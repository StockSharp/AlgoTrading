# Modelo de Estratégia Baseada em R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em RSI com dimensionamento de posição gerenciado por risco e tipos de stop configuráveis.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando o RSI cruza abaixo de `OversoldLevel`.
  - Vendido quando o RSI cruza acima de `OverboughtLevel`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit usando o múltiplo `TpRValue`.
- **Stops**:
  - Fixed, Atr, Percentage ou Ticks.
- **Valores padrão**:
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Variable
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
