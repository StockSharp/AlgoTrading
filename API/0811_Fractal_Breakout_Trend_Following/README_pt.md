# Seguidor de Tendência por Rompimento Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O seguidor de tendência por rompimento fractal entra com uma ordem de compra stop acima de um fractal altista ativado quando a volatilidade está baixa.

## Detalhes

- **Critérios de entrada**: Fractal ascendente acima dos dentes do Alligator e percentil médio do ATR abaixo do limite; ordem de compra stop no nível do fractal.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss no maior entre o stop percentual ou a ativação do fractal descendente.
- **Stops**: Sim.
- **Valores padrão**:
  - `StopLossPercent` = 0.03
  - `AtrThreshold` = 50
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromHours(1)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Fractal, SMMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
