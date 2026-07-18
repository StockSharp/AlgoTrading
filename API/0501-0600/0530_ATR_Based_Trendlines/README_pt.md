# Linhas de Tendência Baseadas em ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que constrói linhas de tendência baseadas em ATR a partir de pontos pivô e opera seus rompimentos.

## Detalhes

- **Critérios de entrada**: Rompimento de linhas de tendência baseadas em ATR.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Rompimento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LookbackLength` = 30
  - `AtrPercent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Price Action
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
