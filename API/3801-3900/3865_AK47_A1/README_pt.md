# Estratégia AK47 A1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porto do especialista "AK47_A1" MetaTrader. A estratégia combina Bill Williams' Alligator, oscilador DeMarker, filtro Williams %R e gatilhos fractais para negociar rompimentos somente quando o mercado sai de condições variáveis.

## Detalhes
- **Dados**: velas de preço definidas por `CandleType`.
- **Indicadores**:
  - Alligator mandíbula/dentes/lábios são SMMAs do período 13/8/5 deslocados em barras 8/5/3 e alimentados com preço médio.
  - O DeMarker com período 13 deve estar na posição comprada de 0,5 para compras e abaixo de 0,5 para vendas.
  - Williams %R com período 14 é normalizado para `[0;1]`; a barra anterior deve ficar entre 0,25 e 0,75 para evitar estados de sobrecompra/sobrevenda.
  - Fractals são detectados nos últimos 5 máximos e mínimos e permanecem válidos por três barras.
- **Critérios de entrada**:
  - Todas as três linhas Alligator devem ser separadas por pelo menos `SpanGatorPoints` pontos (tanto no alinhamento de alta quanto de baixa).
  - **Longo**: O fractal inferior mais recente é recente, DeMarker ≥ 0,5 e o filtro Williams %R aprova a negociação.
  - **Curto**: O fractal superior mais recente é recente, DeMarker ≤ 0,5 e o filtro Williams %R aprova a negociação.
  - As posições opostas são achatadas antes de abrir uma nova.
- **Critérios de saída**:
  - Stop-loss e take-profit definidos por `StopLossPoints` e `TakeProfitPoints` (convertidos em preços absolutos por meio da etapa do instrumento).
  - Trailing stop opcional que segue o fechamento em `TrailingStopPoints` pontos quando a posição se move a favor.
  - Quando aparece um sinal reverso, a posição atual é fechada antes de abrir a nova.
- **Padrões**:
  - `SpanGatorPoints` = 0,5
  - `TakeProfitPoints` = 100
  - `StopLossPoints` = 0 (desativado)
  - `TrailingStopPoints` = 50
  - `CandleType` = velas de 1 hora
