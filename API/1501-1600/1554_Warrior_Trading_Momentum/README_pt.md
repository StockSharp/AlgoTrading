# Estratégia de Momentum Warrior Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de momentum inspirada no Warrior Trading que combina detecção de gaps, VWAP e configurações de vermelho para verde.

## Detalhes

- **Critérios de entrada**: Gap-and-go, red-to-green ou rebote no VWAP com pico de volume.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop baseado em ATR, alvo de lucro e trailing.
- **Stops**: Sim.
- **Valores padrão**:
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: VWAP, RSI, EMA, ATR, Volume
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
