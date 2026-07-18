# MA com Função Logística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

MA com Função Logística é uma estratégia de média móvel que usa uma MA rápida e uma lenta para entradas e suporta saídas baseadas em percentual ou em probabilidade logística.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento > MA rápida e MA rápida > MA lenta.
  - **Vendido**: Fechamento < MA rápida e MA rápida < MA lenta.
- **Critérios de saída**: Metas percentuais ou limiares de probabilidade logística.
- **Stops**: Saídas baseadas em percentual ou probabilidade logística.
- **Valores padrão**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `MaType` = MaTypeEnum.EMA
  - `ExitType` = ExitTypeEnum.Percent
  - `TakeProfitPercent` = 20
  - `StopLossPercent` = 5
  - `LogisticSlope` = 10
  - `LogisticMidpoint` = 0
  - `TakeProfitProbability` = 0.8
  - `StopLossProbability` = 0.2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: MA
  - Complexidade: Baixo
  - Nível de risco: Médio
