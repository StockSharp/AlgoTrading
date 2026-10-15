# Estratégia de Detecção de Outliers com Intervalos de Confiança N-Sigma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia identifica outliers nas variações de preço usando intervalos de confiança N-sigma e opera reversão à média quando ocorrem movimentos extremos.

## Detalhes

- **Critérios de entrada**:
  - Vendido quando z-score > `SecondLimit`.
  - Comprado quando z-score < -`SecondLimit`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Fechar a posição quando |z-score| < `FirstLimit`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: StandardDeviation, Z-Score
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Nível de risco: Médio
