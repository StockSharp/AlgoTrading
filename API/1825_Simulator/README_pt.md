# Estratégia Simulador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera cruzamentos de EMA com stop-loss e take-profit opcionais. Compra quando a EMA rápida cruza acima da EMA lenta e vende quando a EMA rápida cruza abaixo da EMA lenta. Sinais opostos ou alvos de preço fecham posições.

## Detalhes

- **Critérios de entrada**:
  - Comprado: EMA rápida cruza acima da EMA lenta
  - Vendido: EMA rápida cruza abaixo da EMA lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Cruzamento de EMA oposto
  - Comprado: o preço atinge o take-profit ou o stop-loss
  - Vendido: o preço atinge o take-profit ou o stop-loss
- **Stops**: Deslocamentos de preço fixos
- **Valores padrão**:
  - `FastPeriod` = 13
  - `SlowPeriod` = 50
  - `StopLoss` = 0.005m
  - `TakeProfit` = 0.005m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
