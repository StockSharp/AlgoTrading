# Simple MA ADX EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina uma EMA com o Índice Direcional Médio para confirmar a força da tendência.

Compra quando a EMA está subindo, o fechamento anterior está acima da EMA, ADX ultrapassa um limiar e +DI é maior que -DI. Vende quando as condições opostas aparecem. Os níveis de stop-loss e take-profit gerenciam o risco.

## Detalhes

- **Critérios de entrada**: Direção da EMA, preço vs EMA, ADX, +DI/-DI.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou ordens de proteção.
- **Stops**: Sim.
- **Valores padrão**:
  - `AdxPeriod` = 8
  - `MaPeriod` = 8
  - `AdxThreshold` = 22m
  - `StopLoss` = 30m
  - `TakeProfit` = 100m
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, ADX
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
