# Estratégia de Tendência Chande Kroll
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza o stop Chande Kroll com um filtro de tendência SMA. Uma posição comprada é aberta quando o fechamento cruza acima do stop inferior e está acima da SMA. A posição é fechada quando o fechamento cai abaixo do stop superior. O tamanho da posição é baseado no menor fechamento em 1560 barras e no multiplicador de risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `previous close <= previous low stop && Close > low stop && Close > SMA`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - Comprado: `Close < high stop`
- **Stops**: Stop Chande Kroll (extremos Donchian ± ATR)
- **Valores padrão**:
  - `CalcMode` = CalcMode.Exponential
  - `RiskMultiplier` = 5m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `StopLength` = 21
  - `SmaLength` = 21
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: ATR, Donchian, SMA, Lowest
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
