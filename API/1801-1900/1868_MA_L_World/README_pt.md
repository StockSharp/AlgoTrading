# Estratégia MA L World
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de médias móveis ponderadas com trailing stop baseado em EMA.

Abre uma posição comprada quando a WMA rápida cruza acima da WMA lenta. Abre uma posição vendida quando a WMA rápida cruza abaixo da WMA lenta. Utiliza uma EMA de 92 períodos como saída trailing e níveis fixos de stop loss e take profit.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `WMA Rápida` cruza acima da `WMA Lenta`
  - Vendido: `WMA Rápida` cruza abaixo da `WMA Lenta`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento oposto ou preço cruzando a EMA trailing
- **Stops**: Stop loss e take profit via `StartProtection`
- **Valores padrão**:
  - `FastMaLength` = 12
  - `SlowMaLength` = 25
  - `TrailingMaPeriod` = 92
  - `StopLoss` = 95m
  - `TakeProfit` = 670m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: WMA, EMA
  - Stops: Stop loss, take profit, EMA trailing
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
