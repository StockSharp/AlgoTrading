# Estratégia Supertrend Hombrok Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Supertrend com filtros de volume, tamanho do corpo e RSI, com stop e take profit baseados em ATR.

## Detalhes
- **Critérios de entrada**: Tendência de alta com filtros de volume e corpo e RSI abaixo de sobrecompra para comprados; tendência de baixa com filtros e RSI acima de sobrevendido para vendidos
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss ou take profit baseados em ATR
- **Stops**: Stop fixo e take profit do ATR
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70m
  - `RsiOversold` = 30m
  - `VolumeMultiplier` = 1.2m
  - `BodyPctOfAtr` = 0.3m
  - `RiskRewardRatio` = 2m
  - `CapitalPerTrade` = 10m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Supertrend, RSI, ATR, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
