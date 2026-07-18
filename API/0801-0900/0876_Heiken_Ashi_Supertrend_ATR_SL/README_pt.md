# Estratégia Heiken Ashi Supertrend ATR-SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina velas Heikin Ashi com um filtro de direção Supertrend. As entradas requerem velas sem sombras e permitem habilitar stop loss baseado em ATR e ponto de equilíbrio.

## Detalhes

- **Critérios de entrada**:
  - Comprado: vela HA verde sem sombra inferior, filtro de tendência de alta opcional
  - Vendido: vela HA vermelha sem sombra superior, filtro de tendência de baixa opcional
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: vela HA vermelha sem sombra superior ou stop atingido
  - Vendido: vela HA verde sem sombra inferior ou stop atingido
- **Stops**: Baseado em ATR com ponto de equilíbrio opcional
- **Valores padrão**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `AtrFactor` = 3m
  - `UseBreakEven` = false
  - `BreakEvenAtrMultiplier` = 1m
  - `UseHardStop` = false
  - `StopLossAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, Supertrend, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
