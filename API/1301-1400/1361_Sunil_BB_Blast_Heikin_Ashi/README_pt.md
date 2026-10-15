# Estratégia Sunil BB Blast com Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina o rompimento das Bandas de Bollinger com a confirmação de velas Heikin Ashi.

A estratégia aguarda um rompimento das Bandas de Bollinger alinhado com a direção da Heikin Ashi anterior e da vela padrão. As posições usam a banda oposta como stop e um alvo baseado na relação risco-recompensa.

## Detalhes

- **Critérios de entrada**: O preço rompe as Bandas de Bollinger com a Heikin Ashi anterior e a vela na mesma direção.
- **Comprado/Vendido**: Configurável via `Direction`.
- **Critérios de saída**: Tomada de lucro ou stop-loss baseado nas bandas.
- **Stops**: Banda de Bollinger e relação risco/recompensa.
- **Valores padrão**:
  - `BollingerPeriod` = 19
  - `BollingerMultiplier` = 2m
  - `RiskRewardRatio` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Direction` = TradeDirection.Both
  - `SessionBegin` = 09:20:00
  - `SessionEnd` = 15:00:00
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger, HeikinAshi
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
