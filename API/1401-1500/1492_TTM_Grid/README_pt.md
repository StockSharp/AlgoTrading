# Estratégia de Grade TTM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Grade TTM constrói grades de compra e venda baseadas em um estado TTM simples derivado da EMA de máximas e mínimas. A grade é redefinida quando o estado muda, e ordens são colocadas sempre que o preço toca um nível da grade.

## Detalhes

- **Critérios de entrada**: O preço atinge o nível da grade de acordo com o estado TTM.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Nenhum (posições se acumulam).
- **Stops**: Não.
- **Valores padrão**:
  - `TtmPeriod` = 6
  - `GridLevels` = 5
  - `GridSpacing` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Grid
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
