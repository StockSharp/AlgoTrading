# Estratégia Ichimoku by FarmerBTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Ichimoku by FarmerBTC abre posições compradas quando o preço negocia acima da nuvem Ichimoku, a nuvem é altista, uma SMA de período superior confirma a tendência de alta e o volume supera sua média móvel multiplicada por um fator. A saída ocorre quando o preço cai abaixo da nuvem.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `TenkanPeriod` = 10
  - `KijunPeriod` = 30
  - `SenkouSpanBPeriod` = 53
  - `SmaLength` = 13
  - `VolumeLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CandleType` = 1 hour
  - `HtfCandleType` = 1 day
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Ichimoku, SMA, Volume
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
