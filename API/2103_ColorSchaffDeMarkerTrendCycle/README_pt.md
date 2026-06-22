# Estratégia de Ciclo de Tendência Color Schaff DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Ciclo de Tendência Color Schaff DeMarker** usa um oscilador personalizado derivado de valores rápidos e lentos de DeMarker. O indicador aplica duas etapas estocásticas para criar um valor de ciclo que oscila entre -100 e +100. As cores são atribuídas com base no nível e na inclinação do oscilador, que são usadas para gerar sinais de negociação.

A estratégia entra em posições compradas quando o oscilador sai da zona superior e abandona posições vendidas. Abre posições vendidas quando o oscilador sai da zona inferior e abandona posições compradas. A ideia é reagir a mudanças de momentum em níveis extremos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: cor anterior > 5 e cor atual < 6.
  - **Vendido**: cor anterior < 2 e cor atual > 1.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: cor < 2 quando há uma posição comprada aberta.
  - **Vendido**: cor > 5 quando há uma posição vendida aberta.
- **Stops**: Sem stop-loss ou take-profit explícitos.
- **Valores padrão**:
  - `FastDeMarker` = 23
  - `SlowDeMarker` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: DeMarker, Highest, Lowest
  - Stops: Não
  - Complexidade: Médio
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
