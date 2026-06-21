# Estratégia de Rompimento Momentum Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida da estratégia MQL original. Opera rompimentos das Bandas de Bollinger confirmados por EMA, MACD e RSI. A estratégia entra apenas uma vez por expansão de volatilidade e acompanha o stop ao longo da banda média enquanto usa um take profit fixo em pips.

## Detalhes

- **Critérios de entrada**:
  - Comprado: largura de banda acima de `BreakoutFactor`, MACD > 0, RSI > 50, EMA acima da banda média, fechamento anterior acima da banda superior anterior
  - Vendido: largura de banda acima de `BreakoutFactor`, MACD < 0, RSI < 50, EMA abaixo da banda média, fechamento anterior abaixo da banda inferior anterior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: o preço toca o stop trailing da banda média ou atinge o take profit
  - Vendido: o preço toca o stop trailing da banda média ou atinge o take profit
- **Stops**: O nível de stop é a banda média atual de Bollinger, atualizado a cada vela
- **Take Profit**: Distância fixa especificada em pips
- **Valores padrão**:
  - `BollingerLength` = 18
  - `BollingerDeviation` = 2m
  - `BreakoutFactor` = 0.0015m
  - `TakeProfitPips` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Bollinger Bands, EMA, MACD, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
