# Estratégia de Rompimento por Oferta, Demanda e Bloco de Ordens
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento usando níveis de suporte e resistência Donchian com filtro de tendência EMA e confirmação de pico de volume. As posições são protegidas por stop loss e trailing stop.

## Detalhes

- **Critérios de entrada**: Rompimento do canal Donchian com filtro de tendência e volume.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou trailing stop.
- **Stops**: Sim, fixo e trailing.
- **Valores padrão**:
  - `Length` = 20
  - `StopLossTicks` = 1000
  - `TrailingStartTicks` = 2000
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Donchian, EMA, SMA
  - Stops: Fixo e Trailing
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
