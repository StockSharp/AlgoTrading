# Estratégia Tomas Ratio com Análise Multi-Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia acumula ganhos e perdas ponderados em múltiplos períodos para construir um sinal Tomas Ratio. As operações são abertas quando a força do sinal excede um alvo e fechadas quando a fraqueza domina.

## Detalhes

- **Critérios de entrada**: a força do sinal excede o alvo e o preço está acima de EMA(720)
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: os pontos de fechamento excedem os pontos de compra
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = velas de 1 hora
  - `Length` = 720
  - `DeviationLength` = 168
  - `PointsTarget` = 100
  - `UseStandardDeviation` = true
- **Filtros**:
  - Categoria: Momentum
  - Direção: Somente comprado
  - Indicadores: Standard Deviation, SMA, EMA
  - Stops: Não
  - Complexidade: Avançado
  - Período: Múltiplos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
