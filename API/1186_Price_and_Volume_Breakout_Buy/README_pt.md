# Estratégia de Compra por Rompimento de Preço e Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra quando preço e volume simultaneamente rompem acima de suas respectivas máximas do período de lookback enquanto o preço permanece acima da SMA de tendência. Operações vendidas são acionadas quando o preço cai abaixo da mínima do lookback sob a mesma condição de volume e filtro SMA. As posições fecham após cinco fechamentos consecutivos no lado oposto da SMA.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Close > máxima mais alta anterior && Volume > volume mais alto anterior && Close > SMA
  - **Vendido**: Close < mínima mais baixa anterior && Volume > volume mais alto anterior && Close < SMA
- **Comprado/Vendido**: Configurável
- **Critérios de saída**:
  - **Tendência**: Cinco fechamentos além da SMA
- **Stops**: Não
- **Valores padrão**:
  - `PriceBreakoutPeriod` = 60
  - `VolumeBreakoutPeriod` = 60
  - `TrendlineLength` = 200
  - `OrderDirection` = "Long"
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Configurável
  - Indicadores: Highest, SMA, Volume
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
