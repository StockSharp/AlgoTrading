# Estratégia Supertrend EMA Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina Supertrend com confirmação de tendência por EMA e filtro de volume. Entra nas reversões do Supertrend quando o preço está acima ou abaixo da EMA e o volume supera sua EMA. Implementa stop loss baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Supertrend vira para cima, preço acima da EMA, volume acima da Volume EMA
  - Vendido: Supertrend vira para baixo, preço abaixo da EMA, volume acima da Volume EMA
- **Comprado/Vendido**: Configurável
- **Critérios de saída**: Reversão do Supertrend ou stop loss baseado em ATR
- **Stops**: Múltiplo de ATR
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Supertrend, EMA, Volume EMA, ATR
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
