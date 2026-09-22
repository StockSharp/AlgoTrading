# MA com Função Logística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Apesar do nome histórico, esta implementação é uma estratégia de cruzamento de duas EMAs e não calcula um modelo logístico. Cruzamentos em velas finalizadas abrem ou invertem a posição, e take-profit e stop-loss percentuais são medidos a partir dos preços de execução.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: a EMA rápida cruza acima da EMA lenta.
  - **Vendido**: a EMA rápida cruza abaixo da EMA lenta.
- **Critérios de saída**: `TakeProfitPercent` ou `StopLossPercent` é atingido; um cruzamento oposto inverte a posição.
- **Pausa**: cinco velas finalizadas após cada ordem de cruzamento.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 25
  - `TakeProfitPercent` = 8
  - `StopLossPercent` = 5
  - `CandleType` = 20 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: MA
  - Complexidade: Baixo
  - Nível de risco: Médio
