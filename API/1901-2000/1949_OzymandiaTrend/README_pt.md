# OzymandiaTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Ozymandias. O indicador combina ATR com médias móveis de máximas e mínimas para construir um canal dinâmico. Quando a direção muda de baixista para altista, a estratégia compra e fecha posições vendidas. Uma mudança para baixista vende e fecha posições compradas. Parâmetros opcionais de take profit e stop loss gerenciam o risco.

## Detalhes

- **Critérios de entrada**: Mudança de direção do indicador Ozymandias.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stops configurados.
- **Stops**: Take profit e stop loss.
- **Valores padrão**:
  - `Length` = 2
  - `CandleType` = TimeSpan.FromHours(4)
  - `TakeProfitPoints` = 2000
  - `StopLossPoints` = 1000
  - `BuyEntry` = true
  - `SellEntry` = true
  - `BuyExit` = true
  - `SellExit` = true
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Ozymandias (ATR + MA)
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
