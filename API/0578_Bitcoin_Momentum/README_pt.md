# Estratégia de Momentum de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de momentum para Bitcoin que opera apenas quando o preço está acima de uma EMA de período superior e evita condições de precaução. Um stop trailing baseado em ATR protege os lucros.

## Detalhes

- **Critérios de entrada**: Preço acima da EMA semanal e sem condição de precaução.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Preço abaixo do stop trailing ou da EMA semanal.
- **Stops**: Stop trailing baseado em ATR.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `EmaLength` = 20
  - `AtrLength` = 5
  - `TrailStopLookback` = 7
  - `TrailStopMultiplier` = 0.2m
  - `StartTime` = 2000-01-01
  - `EndTime` = 2099-01-01
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: EMA, ATR, Highest
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
