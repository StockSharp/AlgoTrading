# Estratégia IU de Cruzamento de MA em Período Superior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia IU Higher Timeframe MA Cross opera quando uma média móvel rápida calculada em um período selecionado pelo usuário cruza uma média móvel mais lenta de possivelmente outro período. Uma posição comprada é aberta em um cruzamento de alta e uma posição vendida em um cruzamento de baixa. O stop-loss é colocado no extremo da vela anterior e o take profit utiliza uma relação risco/recompensa configurável.

## Detalhes
- **Dados**: Velas de períodos especificados.
- **Critérios de entrada**:
  - **Comprado**: MA1 cruza acima de MA2.
  - **Vendido**: MA1 cruza abaixo de MA2.
- **Critérios de saída**: Stop-loss ou take profit atingido.
- **Stops**: Máximo/mínimo da vela anterior com multiplicador `RiskToReward`.
- **Valores padrão**:
  - `Ma1CandleType` = 60m
  - `Ma1Length` = 20
  - `Ma1Type` = MovingAverageTypeEnum.Exponential
  - `Ma2CandleType` = 60m
  - `Ma2Length` = 50
  - `Ma2Type` = MovingAverageTypeEnum.Exponential
  - `RiskToReward` = 2
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado & Vendido
  - Indicadores: Média Móvel
  - Complexidade: Baixo
  - Nível de risco: Médio
