# Comparação Multi-Banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Comparação Multi-Banda utiliza SMA, desvio padrão e bandas de quantis de preço. A estratégia vai comprada quando o preço fecha acima do quantil superior menos o desvio padrão por um número definido de barras e sai quando o preço cai abaixo desse nível por um número definido de barras.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento acima de (quantil superior - desvio padrão) por `EntryConfirmBars` barras.
- **Critérios de saída**: Fechamento abaixo dessa linha por `ExitConfirmBars` barras.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 20
  - `BollingerMultiplier` = 1
  - `UpperQuantile` = 0.95
  - `EntryConfirmBars` = 1
  - `ExitConfirmBars` = 1
- **Filtros**:
  - Categoria: Estatística
  - Direção: Comprado
  - Indicadores: SMA, Standard Deviation
  - Complexidade: Moderado
  - Nível de risco: Médio
