# Estratégia Zero Lag MACD + Kijun-sen + EOM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina Zero Lag MACD com a linha base Kijun-sen e o filtro Ease of Movement. Usa stop e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**: Cruzamento de MACD com filtros de Kijun-sen e EOM.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop ou take profit baseados em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD, Donchian, EOM, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
