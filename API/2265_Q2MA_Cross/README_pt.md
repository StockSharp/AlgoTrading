# Estratégia de Cruzamento Q2MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Cruzamento Q2MA opera com base no cruzamento de médias móveis suavizadas construídas sobre os preços de fechamento e abertura das velas. Uma posição comprada é aberta quando a média do fechamento cai abaixo da média da abertura após ter estado acima, enquanto uma posição vendida é aberta no cruzamento oposto. As posições são fechadas quando uma tendência contrária aparece. A estratégia também aplica níveis de stop loss e take profit medidos em ticks.

## Detalhes

- **Critérios de entrada**: cruzamento entre médias móveis dos preços de fechamento e abertura
- **Comprado/Vendido**: ambas as direções
- **Critérios de saída**: cruzamento oposto ou stop loss/take profit
- **Stops**: sim
- **Valores padrão**:
  - `Length` = 8
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Volume` = 1
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `Invert` = false
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Moving Average
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
