# Estratégia CBC com Confirmação de Tendência e Stop Loss Separado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o estado de mudança de cor de barra (CBC) para detectar reversões quando o preço rompe a máxima ou mínima do candle anterior. As entradas requerem confirmação de tendência via EMA e VWAP e são restritas a uma janela de sessão de trading. As saídas aplicam um alvo de lucro baseado em ATR e usam as extremidades do candle anterior como níveis de stop loss.

## Detalhes

- **Critérios de entrada**: Reversões CBC, filtro opcional de reversões fortes, EMA lenta relativa ao VWAP, dentro do horário de negociação.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take-profit com multiplicador ATR, stop loss na máxima/mínima do candle anterior.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrLength` = 14
  - `ProfitTargetMultiplier` = 1.0m
  - `StrongFlipsOnly` = true
  - `EntryStartHour` = 10
  - `EntryEndHour` = 15
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, VWAP, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
