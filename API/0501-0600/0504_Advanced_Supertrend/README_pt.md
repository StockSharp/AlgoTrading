# Estratégia Avançada de Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Avançada de Supertrend aprimora o indicador clássico Supertrend com filtros opcionais de RSI, média móvel e força de tendência. Entra comprado quando o Supertrend muda para altista e entra vendido quando fica baixista. Stop loss e take profit opcionais são derivados de múltiplos de ATR.

## Detalhes

- **Critérios de entrada**:
  - Supertrend muda de direção (baixista→altista para comprado, altista→baixista para vendido).
  - Filtros opcionais: RSI dentro dos limites definidos, preço relativo a uma média móvel, força de tendência e confirmação de rompimento.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal oposto do Supertrend ou níveis opcionais de stop-loss/take-profit.
- **Stops**: Stop loss e take profit opcionais baseados em ATR.
- **Valores padrão**:
  - `AtrLength` = 6
  - `Multiplier` = 3.0
  - `UseRsiFilter` = false
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseMaFilter` = true
  - `MaLength` = 50
  - `MaType` = Weighted
  - `UseStopLoss` = true
  - `SlMultiplier` = 3.0
  - `UseTakeProfit` = true
  - `TpMultiplier` = 9.0
  - `UseTrendStrength` = false
  - `MinTrendBars` = 2
  - `UseBreakoutConfirmation` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: Supertrend, RSI, Média Móvel
  - Stops: Baseado em ATR
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
