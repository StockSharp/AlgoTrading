# Estratégia de Densidade Léxica em Documentos Regulatórios
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de fator examina a linguagem utilizada em documentos regulatórios para avaliar o desempenho futuro das ações. A densidade léxica é medida como a fração de termos únicos no relatório mais recente. Documentos densos sugerem divulgações ricas e cheias de informações que frequentemente precedem retornos mais fortes, enquanto uma redação esparsa pode mascarar fraquezas.

A cada trimestre, o universo é ordenado por densidade léxica. O quintil mais alto é mantido comprado e o quintil mais baixo é vendido a descoberto, com posições de peso igual. O rebalanceamento ocorre durante os primeiros três dias úteis de fevereiro, maio, agosto e novembro, e as posições permanecem abertas entre as revisões sem stops.

Testes retrospectivos em ações americanas amplas mostram que o fator fornece um prêmio estável com rotatividade moderada, tornando-o um componente útil em carteiras multifator.

## Detalhes

- **Critérios de entrada**: Ordenação trimestral por densidade léxica; comprado no quintil superior,
  vendido no quintil inferior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Próximo rebalanceamento
- **Stops**: Não
- **Valores padrão**:
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Análise de texto
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Multimensal
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
