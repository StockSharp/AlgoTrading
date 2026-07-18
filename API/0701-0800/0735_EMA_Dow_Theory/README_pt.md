# Estratégia EMA com Teoria de Dow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um cruzamento de Média Móvel Exponencial (EMA) rápida e lenta com um filtro de tendência básico da Teoria de Dow. A tendência é determinada pelos topos e fundos de oscilação recentes. As posições são tomadas quando as EMAs se alinham com a direção da tendência.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida ≥ EMA lenta e o preço rompe acima do último topo de oscilação.
  - **Vendido**: EMA rápida < EMA lenta e o preço rompe abaixo do último fundo de oscilação.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Comprimento da EMA rápida = 47
  - Comprimento da EMA lenta = 50
  - Comprimento de oscilação = 6 barras
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, topo/fundo de oscilação
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
