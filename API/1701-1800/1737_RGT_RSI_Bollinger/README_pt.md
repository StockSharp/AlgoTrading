# Estratégia RGT RSI Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o Índice de Força Relativa (RSI) com as Bandas de Bollinger para identificar oportunidades de reversão à média. Uma posição comprada é aberta quando o RSI indica um mercado sobrevendido e o preço opera abaixo da banda inferior de Bollinger. Uma posição vendida é iniciada quando o RSI mostra um mercado sobrecomprado e o preço sobe acima da banda superior. A estratégia aplica um stop-loss inicial e depois usa trailing no stop assim que um lucro mínimo é alcançado.

O trailing stop assegura os lucros seguindo o preço a uma distância fixa quando o trade se move favoravelmente. As posições são fechadas quando o trailing stop é acionado.

## Detalhes

- **Critérios de entrada**: RSI abaixo de `RsiLow` e preço abaixo da banda inferior para comprados; RSI acima de `RsiHigh` e preço acima da banda superior para vendidos.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Acionamento do trailing stop.
- **Stops**: Stop-loss inicial e trailing stop.
- **Valores padrão**:
  - `RsiPeriod` = 8
  - `RsiHigh` = 90
  - `RsiLow` = 10
  - `StopLossPips` = 70
  - `TrailingStopPips` = 35
  - `MinProfitPips` = 30
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, Bandas de Bollinger
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
