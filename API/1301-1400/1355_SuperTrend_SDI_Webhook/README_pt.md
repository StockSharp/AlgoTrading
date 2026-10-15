# Estratégia SuperTrend SDI Webhook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no SuperTrend e no Indicador Direcional Suavizado (SDI). Entra comprado quando +DI está acima de -DI e o SuperTrend indica uma tendência de alta. Posições vendidas são abertas quando -DI está acima de +DI e o SuperTrend aponta para baixo. A estratégia aplica take profit, stop-loss e trailing stop em porcentagem.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `+DI > -DI && SuperTrend up`
  - Vendido: `-DI > +DI && SuperTrend down`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Take profit, stop-loss ou trailing stop
- **Indicadores**: SuperTrend, AverageDirectionalIndex
- **Stops**: Take profit, stop-loss e trailing stop em porcentagem
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 1.8m
  - `DiLength` = 3
  - `DiSmooth` = 7
  - `TakeProfitPercent` = 25m
  - `StopLossPercent` = 4.8m
  - `TrailingPercent` = 1.9m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, SDI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
