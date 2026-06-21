# Estratégia de Razão de Volatilidade Histórica (HVR)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na Razão de Volatilidade Histórica (HVR). Compara a volatilidade de curto prazo em 6 barras com a volatilidade de longo prazo em 100 barras usando retornos logarítmicos. Quando a razão sobe acima do limiar, o sistema vai comprado esperando expansão de volatilidade. Quando cai abaixo do limiar, o sistema vai vendido.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `HVR > RatioThreshold`
  - Vendido: `HVR < RatioThreshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `ShortPeriod` = 6
  - `LongPeriod` = 100
  - `RatioThreshold` = 1.0
  - `CandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: Volatilidade histórica (curta e longa)
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
