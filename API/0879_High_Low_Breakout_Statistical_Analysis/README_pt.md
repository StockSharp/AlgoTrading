# Estratégia de Rompimento de Máximos e Mínimos com Análise Estatística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera rompimentos dos níveis de máximo ou mínimo do período selecionado. A estratégia pode entrar comprada ou vendida com base na opção configurada e fecha a posição após um número fixo de barras.

## Detalhes

- **Critérios de entrada**: O fechamento cruza o nível de máximo ou mínimo selecionado.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou após HoldingPeriod barras.
- **Stops**: Não.
- **Valores padrão**:
  - `EntryOption` = LongAtHigh
  - `TimeframeOption` = Daily
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: High, Low
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
