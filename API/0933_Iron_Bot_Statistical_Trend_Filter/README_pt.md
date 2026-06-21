# Estratégia Iron Bot de Filtro de Tendência Estatística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera rompimentos baseados em níveis de tendência estatística calculados a partir de intervalos de Fibonacci e Z-score.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço cruza acima da linha de tendência e do nível de tendência alto com Z-score não negativo.
  - **Vendido**: o preço cruza abaixo da linha de tendência e do nível de tendência baixo com Z-score não positivo.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss em `SlRatio` por cento da entrada.
  - Take-profit em um dos quatro níveis (`Tp1Ratio`–`Tp4Ratio`) da entrada.
- **Stops**: Sim.
- **Valores padrão**:
  - `ZLength` = 40.
  - `AnalysisWindow` = 44.
  - `HighTrendLimit` = 0.236.
  - `LowTrendLimit` = 0.786.
  - `EmaLength` = 200.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Z-score, EMA, ação do preço
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
