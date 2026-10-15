# Cruzamento EMA 34 com Stop Loss no Ponto de Equilíbrio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **EMA 34 Crossover with Break Even Stop Loss** entra comprado quando o preço cruza acima da EMA de 34 períodos. O stop loss é colocado na mínima da vela anterior, o take profit é dez vezes o risco, e o stop é movido para o ponto de equilíbrio depois que o preço atinge três vezes o risco.

## Detalhes
- **Critérios de entrada**: O fechamento cruza acima de EMA(34) de baixo para cima.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop loss na mínima anterior ou take profit em 10× o risco.
- **Stops**: Sim, stop de ponto de equilíbrio.
- **Valores padrão**:
  - `EmaPeriod = 34`
  - `TakeProfitMultiplier = 10m`
  - `BreakEvenMultiplier = 3m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
