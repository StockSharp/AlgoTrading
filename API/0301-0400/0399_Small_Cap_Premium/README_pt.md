# Estratégia de Prêmio de Pequena Capitalização
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Prêmio de Pequena Capitalização** captura a tendência histórica das ações de baixa capitalização de superar as de grande capitalização. O universo é dividido por capitalização de mercado, e a carteira mantém uma cesta de small caps enquanto vende a descoberto um índice de large caps.

## Detalhes
- **Critérios de entrada**: Seleção por classificação de capitalização de mercado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rebalanceamento periódico.
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
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
