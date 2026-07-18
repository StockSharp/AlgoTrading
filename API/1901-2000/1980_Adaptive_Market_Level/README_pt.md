# Nível de Mercado Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera com base no indicador Adaptive Market Level (AML). O indicador se adapta à volatilidade atual e traça um nível de preço dinâmico. Uma posição comprada é aberta quando a linha AML vira para cima e uma posição vendida quando vira para baixo. Posições opostas são fechadas em uma mudança de cor ou quando o stop-loss/take-profit é acionado.

O sistema segue tendências de médio prazo e funciona em períodos mais altos por padrão.

## Detalhes

- **Critérios de entrada**: A linha AML muda de direção para cima para comprados e para baixo para vendidos.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Mudança de direção do AML ou stop/alvo.
- **Stops**: Sim.
- **Valores padrão**:
  - `Fractal` = 6
  - `Lag` = 7
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Adaptive Market Level
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
