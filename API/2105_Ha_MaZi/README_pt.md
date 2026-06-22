# Ha MaZi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina velas Heikin Ashi, um filtro EMA e confirmação de pivô ZigZag. Uma operação comprada é aberta quando uma vela Heikin Ashi de alta se forma em uma nova mínima ZigZag acima da EMA. Posições vendidas aparecem em uma vela de baixa em uma nova máxima ZigZag abaixo da EMA. As posições são fechadas por stop loss fixo ou take profit.

## Detalhes
- **Critérios de entrada**: Pivô ZigZag com direção Heikin Ashi e filtro EMA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Stop e alvo fixos.
- **Valores padrão**:
  - `MaPeriod` = 40
  - `ZigzagLength` = 13
  - `StopLoss` = 70
  - `TakeProfit` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, EMA, ZigZag
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
