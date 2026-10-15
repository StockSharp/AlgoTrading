# Estratégia de Swing Trader Coensio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento de linhas de tendência definidas pelo usuário. A estratégia calcula projeções lineares a partir dos parâmetros de inclinação e intercepto para linhas de alta e de baixa. Quando o preço de fechamento supera a linha de compra projetada por um limiar, uma posição comprada é aberta. Quando o preço cai abaixo da linha de venda menos o limiar, uma posição vendida é aberta.

As posições são protegidas por valores de take profit e stop loss em ticks. Um stop trailing opcional atualiza o stop de proteção conforme o preço se move a favor. Uma opção adicional fecha a operação se o rompimento falhar na próxima vela.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > BuyLine + EntryThreshold`
  - Vendido: `Close < SellLine - EntryThreshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss, take profit, stop trailing ou sinal oposto
- **Stops**:
  - Take profit em ticks
  - Stop loss em ticks
  - Stop trailing opcional em ticks
  - Fechamento opcional por falso rompimento na próxima vela
- **Valores padrão**:
  - `EntryThreshold` = 15m
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 100
  - `EnableTrailing` = false
  - `TrailingStepTicks` = 5
  - `FalseBreakClose` = true
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BuyLineSlope` = 0m
  - `BuyLineIntercept` = 0m
  - `SellLineSlope` = 0m
  - `SellLineIntercept` = 0m
- **Filtros**:
  - Categoria: Rompimento de linha de tendência
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Médio
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
