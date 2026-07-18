# Estratégia de Média Móvel Hull Triangular
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento da Média Móvel Hull com um atraso de duas barras.

A estratégia compara a Média Móvel Hull com seu valor de duas barras atrás. Um cruzamento para cima abre uma posição comprada, enquanto um cruzamento para baixo abre uma posição vendida. A direção pode ser limitada ao modo somente comprado ou somente vendido.

## Detalhes
- **Critérios de entrada**: Cruzamento de HMA com atraso de 2 barras.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Sinal oposto ou filtro de direção.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 40
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `EntryMode` = EntryDirection.LongAndShort
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: MA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
