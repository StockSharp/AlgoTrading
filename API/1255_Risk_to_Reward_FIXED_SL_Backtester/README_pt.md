# Estratégia de Backtesting de Risco/Retorno com SL Fixo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado quando o preço de fechamento corresponde a um valor definido pelo usuário. O stop loss é definido pelo ATR ou mínimo de pivô e o take profit usa uma relação risco/retorno ou percentual fixo. Opcionalmente move o stop para breakeven após atingir um alvo.

## Detalhes

- **Critérios de entrada**: preço de fechamento igual a `DealStartValue`
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: take profit ou stop loss (breakeven opcional)
- **Stops**: ATR ou mínimo de pivô com breakeven
- **Valores padrão**:
  - `DealStartValue` = 100
  - `UseRiskToReward` = true
  - `RiskToRewardRatio` = 1.5
  - `StopLossType` = Atr
  - `AtrFactor` = 1.4
  - `PivotLookback` = 8
  - `FixedTp` = 0.015
  - `FixedSl` = 0.015
  - `UseBreakEven` = true
  - `BreakEvenRr` = 1.0
  - `BreakEvenPercent` = 0.001
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: ATR, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
