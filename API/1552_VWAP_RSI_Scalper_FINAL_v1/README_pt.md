# Estratégia de Scalper VWAP RSI FINAL v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping que combina VWAP e RSI com saídas baseadas em ATR e limites diários de negociações.

## Detalhes

- **Critérios de entrada**: Preço relativo ao VWAP e EMA com limiares de RSI dentro da sessão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop e alvo baseados em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: VWAP, RSI, EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
