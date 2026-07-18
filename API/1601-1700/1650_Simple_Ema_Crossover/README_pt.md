# Estratégia Simples de Cruzamento de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza um cruzamento de duas médias móveis exponenciais com stop-loss e take-profit integrados.

Compra quando a EMA rápida cruza acima da EMA lenta e vende quando cruza abaixo.

## Detalhes

- **Critérios de entrada**: Cruzamento da EMA rápida com a EMA lenta.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto ou ordens stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `Periods` = 17
  - `StopLoss` = 31 (absoluto)
  - `TakeProfit` = 69 (absoluto)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
