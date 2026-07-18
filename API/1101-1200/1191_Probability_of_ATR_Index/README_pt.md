# Estratégia de Índice de Probabilidade de ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Probability of ATR Index.

## Detalhes

- **Critérios de entrada**: A probabilidade cruza acima ou abaixo de sua média móvel.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `AtrDistance` = 1.5m
  - `Bars` = 8
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: ATR, SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
