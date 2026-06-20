# Estratégia do Fator de Qualidade dos Lucros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Earnings Quality Factor** rebalancea anualmente em 1º de julho, comprando ações de alta qualidade e vendendo ações de baixa qualidade com base nas pontuações de qualidade dos lucros.

## Detalhes
- **Critérios de entrada**: Rebalanceamento anual em 1º de julho usando pontuações de qualidade.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Próximo rebalanceamento anual.
- **Stops**: Não.
- **Valores padrão**:
  - `MinTradeUsd = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Qualidade
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
