# Estratégia de Rotação de Momentum Setorial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Rotação de Momentum Setorial** rotaciona capital entre ETFs setoriais. No final de cada mês, o retorno histórico de cada setor em várias janelas de retrospectiva é calculado. O sistema compra os setores mais fortes e sai dos mais fracos, mantendo exposição apenas aos de melhor desempenho.

## Detalhes
- **Critérios de entrada**: Classificação mensal do momentum de ETFs setoriais.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Rebalanceamento mensal quando os rankings mudam.
- **Stops**: Sem stop explícito.
- **Valores padrão**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: Baseados em preço
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
