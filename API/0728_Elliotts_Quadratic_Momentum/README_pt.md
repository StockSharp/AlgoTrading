# Momentum Quadrático de Elliott
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Elliott's Quadratic Momentum** combina múltiplos indicadores SuperTrend para capturar o momentum inspirado nas ondas de Elliott.

A estratégia entra comprado quando todas as quatro linhas SuperTrend sinalizam tendência de alta e entra vendido quando todas sinalizam tendência de baixa. As posições são fechadas quando qualquer SuperTrend reverte sua direção.

## Detalhes
- **Critérios de entrada**: Todos os indicadores SuperTrend alinhados na mesma direção.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Qualquer SuperTrend vira contra a posição.
- **Stops**: Sem stops explícitos.
- **Valores padrão**:
  - `AtrLength1 = 7`
  - `Multiplier1 = 4.0m`
  - `AtrLength2 = 14`
  - `Multiplier2 = 3.618m`
  - `AtrLength3 = 21`
  - `Multiplier3 = 3.5m`
  - `AtrLength4 = 28`
  - `Multiplier4 = 3.382m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
