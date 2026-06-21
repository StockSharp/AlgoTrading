# Estratégia Bjorgum Double Tap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia busca padrões de topo duplo e fundo duplo. Uma operação vendida é aberta quando o preço rompe abaixo da linha de pescoço de um topo duplo, e uma operação comprada quando o preço rompe acima da linha de pescoço de um fundo duplo. Os níveis de alvo e stop são calculados como extensões de Fibonacci da altura do padrão.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Rompimento do fundo duplo acima da linha de pescoço.
  - **Vendido**: Rompimento do topo duplo abaixo da linha de pescoço.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Níveis de stop ou alvo.
- **Stops**: Percentual de Fibonacci via `StopLossFib`.
- **Valores padrão**:
  - Comprimento do pivô 50.
  - Tolerância do pivô 15%.
  - Fibonacci de alvo 100%.
  - Fibonacci de stop 0%.
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Highest/Lowest
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Médio prazo
