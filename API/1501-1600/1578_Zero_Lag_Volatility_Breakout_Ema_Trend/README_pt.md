# Estratégia Zero-lag de Rompimento de Volatilidade com EMA Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento que usa a diferença de EMA sem lag com bandas de Bollinger e um filtro de tendência EMA. Opcionalmente mantém posições até um sinal oposto.

## Detalhes

- **Critérios de entrada**: Dif cruza acima da banda superior com filtro de inclinação EMA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Saída opcional no cruzamento da banda média.
- **Stops**: Sem stops explícitos.
- **Valores padrão**:
  - `EmaLength` = 200
  - `StdMultiplier` = 2m
  - `UseBinary` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, Bollinger Bands
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
