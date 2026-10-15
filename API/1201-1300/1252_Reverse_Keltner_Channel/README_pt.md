# Estratégia de Canal Keltner Inverso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra quando o preço re-entra no canal Keltner vindo de fora e visa a banda oposta, com filtro ADX opcional.

A estratégia vai comprado quando o preço cruza a banda inferior do Keltner de baixo para cima e fecha na banda superior ou num stop colocado na metade da largura do canal. Operações vendidas são simétricas. Um filtro ADX pode restringir operações a regimes de tendência fraca ou forte.

## Detalhes

- **Critérios de entrada**: O preço cruza a banda exterior do Keltner para dentro do canal, filtro ADX opcional.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Banda oposta ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 2m
  - `StopLossFactor` = 0.5m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `UseAdxFilter` = true
  - `WeakTrendOnly` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Keltner, ADX
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
