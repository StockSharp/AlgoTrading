# Estratégia da Máxima de Ontem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento de compra que coloca uma ordem buy stop acima da máxima do dia anterior.
Filtro ROC opcional, stop trailing e fechamento por EMA fornecem controle adicional de risco.

## Detalhes

- **Critérios de entrada**: Fechamento abaixo da máxima de ontem, depois buy stop na máxima + gap
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Stop-loss, take-profit, stop trailing opcional ou cruzamento de EMA
- **Stops**: Sim, baseado em percentagem
- **Valores padrão**:
  - `Gap` = 1
  - `StopLoss` = 3
  - `TakeProfit` = 9
  - `UseRocFilter` = false
  - `RocThreshold` = 1
  - `UseTrailing` = true
  - `TrailEnter` = 2
  - `TrailOffset` = 1
  - `CloseOnEma` = false
  - `EmaLength` = 10
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: Price, ROC, EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
