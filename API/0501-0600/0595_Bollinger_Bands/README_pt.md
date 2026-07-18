# Estratégia de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera rompimentos de Bollinger Bands. Compra quando o preço fecha acima da banda superior e vende quando fecha abaixo da banda inferior. Sai em um cruzamento de média móvel simples ou quando o stop loss é atingido.

## Detalhes

- **Critérios de entrada**:
  - Comprado: fechamento acima da banda superior de Bollinger
  - Vendido: fechamento abaixo da banda inferior de Bollinger
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: fechamento abaixo da SMA ou preço atinge o stop loss
  - Vendido: fechamento acima da SMA ou preço atinge o stop loss
- **Stops**: Percentual do preço de entrada
- **Valores padrão**:
  - `BbLength` = 120
  - `BbDeviation` = 2m
  - `SmaLength` = 110
  - `StopLossPercent` = 6m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Bollinger Bands, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
