# Estratégia de Assimetria Estatística em Commodities
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Assimetria Estatística em Commodities** classifica futuros de commodities pela assimetria da distribuição de seus retornos. Contratos com assimetria positiva são favorecidos para posições compradas, enquanto os com assimetria fortemente negativa são vendidos a descoberto, assumindo que movimentos extremos de baixa reverterão à média.

## Detalhes
- **Critérios de entrada**: Classificação pela assimetria histórica de retornos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rebalanceamento periódico.
- **Stops**: Sem stop explícito.
- **Valores padrão**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Estatístico
  - Direção: Ambos
  - Indicadores: Baseados em preço
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
