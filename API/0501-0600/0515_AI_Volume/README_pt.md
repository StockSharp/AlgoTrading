# Estratégia AI Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia AI Volume busca explosões repentinas de participação. Um pico de volume ocorre quando o volume atual supera sua EMA por um multiplicador dado. Se o pico se alinha com a EMA de preço de 50 períodos e a cor do candle, a estratégia entra nessa direção. Cada operação é fechada após um número fixo de barras.

## Detalhes

- **Critérios de entrada**: Volume > VolumeEMA * VolumeMultiplier e preço acima/abaixo da EMA de 50 com cor de candle correspondente.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Posição fechada após `ExitBars` candles.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `VolumeEmaLength` = 20
  - `VolumeMultiplier` = 2.0
  - `ExitBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento de volume
  - Direção: Ambos
  - Indicadores: EMA, Volume EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
