# Estratégia de Efeito do Interesse a Descoberto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Efeito do Interesse a Descoberto** usa os níveis de interesse a descoberto para prever o desempenho das ações. Valores com poucos dias para cobrir tendem a superar aqueles com muitas posições vendidas. Em intervalo mensal, as ações são ordenadas pelo interesse a descoberto, e a carteira compra o grupo com o menor nível enquanto vende a descoberto o com o maior.

## Detalhes
- **Critérios de entrada**: Classificação mensal por ratio de interesse a descoberto ou dias para cobrir.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rebalanceamento mensal.
- **Stops**: Sem stop explícito.
- **Valores padrão**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Fundamentais
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
