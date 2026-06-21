# Estratégia de Scalping de Pullback em Grade Inteligente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping baseada em grade que expande níveis de preço orientados por ATR a partir de um preço base de vinte barras atrás. Os pullbacks são filtrados com RSI antes das entradas. As posições usam um alvo de lucro e um stop de trailing ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: close < basePrice - (LongLevel + 1) * ATR * GridFactor && range/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - Vendido: close > basePrice + (ShortLevel + 1) * ATR * GridFactor && range/high > NoTradeZone && RSI > MinRsiShort && close < open
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: alvo de lucro ou stop de trailing ATR
- **Stops**: Stop de trailing ATR
- **Valores padrão**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: ATR, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
