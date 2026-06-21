# Estratégia Vendida de Vela de Alerta EMA 5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **EMA 5 Alert Candle Short** aguarda três velas que toquem a EMA de 5 períodos e então identifica uma vela que permanece acima dela. Uma posição vendida é aberta quando a próxima vela rompe a mínima da vela de alerta, com o take profit colocado a uma distância igual ao stop loss.

## Detalhes
- **Critérios de entrada**: após três velas tocando a EMA, vendido no rompimento da mínima de uma vela que não toca a EMA.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: stop loss na máxima da vela de alerta, take profit à mesma distância.
- **Stops**: Sim, baseado no intervalo da vela de alerta.
- **Valores padrão**:
  - `EmaPeriod = 5`
  - `RiskPerTrade = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Vendido
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
