# Estratégia de Bollinger Bands e Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de Bandas de Bollinger filtrados por níveis de Fibonacci. Uma posição comprada se abre quando o preço cruza acima da banda superior e a mínima da vela está acima de um suporte baseado em Fibonacci. Uma posição vendida se abre quando o preço cruza abaixo da banda inferior e a máxima da vela está abaixo de uma resistência baseada em Fibonacci.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento cruza acima da banda superior e mínima > Fibonacci baixo.
  - **Vendido**: O fechamento cruza abaixo da banda inferior e máxima < Fibonacci alto.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: O fechamento cruza abaixo da banda do meio.
  - **Vendido**: O fechamento cruza acima da banda do meio.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2
  - `FibonacciLength` = 50
  - `FibonacciLevel0` = 0
  - `FibonacciLevel100` = 1
- **Filtros**:
  - Categoria: Rompimento de banda
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Highest, Lowest
  - Stops: Nenhum
  - Complexidade: Básico
  - Período: 1H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
