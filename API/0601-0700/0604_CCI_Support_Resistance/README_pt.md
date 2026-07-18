# Estratégia CCI de Suporte e Resistência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa pivôs de CCI para construir níveis dinâmicos de suporte e resistência. Um filtro de tendência baseado no cruzamento ou inclinação da EMA é aplicado antes de operar rompimentos desses níveis.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço fecha acima do suporte baseado em CCI após tocá-lo e a tendência é de alta.
  - Vendido: o preço fecha abaixo da resistência baseada em CCI após tocá-la e a tendência é de baixa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop loss e take profit baseados em ATR.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `CciLength` = 50
  - `LeftPivot` = 50
  - `RightPivot` = 50
  - `Buffer` = 10
  - `TrendMatter` = true
  - `TrendType` = Cross
  - `SlowMaLength` = 100
  - `FastMaLength` = 50
  - `SlopeLength` = 5
  - `Ksl` = 1.1
  - `Ktp` = 2.2
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: CCI, EMA, ATR
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
