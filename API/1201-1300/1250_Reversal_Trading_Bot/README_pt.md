# Estratégia de Bot de Trading de Reversão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Bot de Trading de Reversão utiliza divergência do RSI com filtros opcionais de volume, ADX, Bandas de Bollinger e cruzamento de RSI para capturar reversões de mercado. As posições são protegidas com stop-loss e take-profit de percentual fixo.

## Detalhes

- **Critérios de entrada**: divergência do RSI com filtros opcionais de volume, ADX, Bandas de Bollinger e cruzamento de RSI
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou take-profit
- **Stops**: Percentual fixo
- **Valores padrão**:
  - `RsiLength` = 8
  - `FastRsiLength` = 14
  - `SlowRsiLength` = 21
  - `BbLength` = 20
  - `AdxThreshold` = 20
  - `DivLookback` = 5
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI, ADX, Bollinger Bands, SMA
  - Stops: Fixo
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

