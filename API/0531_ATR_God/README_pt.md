# ATR GOD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina uma entrada de Supertrend com stop loss e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**: Inversão do Supertrend.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop de ATR ou sinal oposto.
- **Stops**: Baseado em ATR.
- **Valores padrão**:
  - `Period` = 10
  - `Multiplier` = 3m
  - `RiskMultiplier` = 4.5m
  - `RewardRiskRatio` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: ATR
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

