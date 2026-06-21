# My Line Order
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia dispara ordens de mercado quando o preço cruza níveis horizontais predefinidos. O usuário especifica níveis separados para entradas compradas e vendidas e parâmetros de risco em pips. Após abrir uma posição, a estratégia rastreia stop-loss, take-profit e trailing stop opcional.

O sistema é adequado para configurações discricionárias onde os níveis de entrada são conhecidos antecipadamente. Funciona com qualquer instrumento e período porque depende apenas de níveis de preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento cruza acima de `BuyPrice`.
  - **Vendido**: O preço de fechamento cruza abaixo de `SellPrice`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss em `StopLossPips`.
  - Take-profit em `TakeProfitPips`.
  - Trailing stop se `TrailingStopPips` > 0.
- **Stops**: Sim, em pips.
- **Valores padrão**:
  - `BuyPrice` = 0 (desativado)
  - `SellPrice` = 0 (desativado)
  - `TakeProfitPips` = 30
  - `StopLossPips` = 20
  - `TrailingStopPips` = 0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Manual
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
