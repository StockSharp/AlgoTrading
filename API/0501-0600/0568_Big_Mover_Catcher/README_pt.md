# Estratégia Caçadora de Grandes Movimentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprada quando o preço fecha acima da Banda de Bollinger superior e todos os filtros habilitados confirmam o movimento. Também pode ficar vendida quando o preço fecha abaixo da banda inferior. Os filtros incluem RSI, ADX, ATR, direção de tendência EMA e MACD. Um stop loss de percentual fixo é aplicado, as posições são fechadas quando o preço retorna à banda do meio e um take profit forçado opcional sai em velas incomumente grandes.

## Detalhes
- **Critérios de entrada:**
  - **Comprado:** fechamento > Banda de Bollinger superior e todos os filtros ativos aprovados.
  - **Vendido:** fechamento < Banda de Bollinger inferior e todos os filtros ativos aprovados.
- **Comprado/Vendido:** Ambos (configurável).
- **Critérios de saída:**
  - O preço cruza a Banda de Bollinger do meio.
  - Take profit forçado opcional em velas grandes.
- **Stops:** Stop loss de percentual fixo.
- **Valores padrão:** Comprimento Bollinger = 40, stop loss = 2%, limite de TP forçado = 5%.
- **Filtros:** RSI (14), ADX (28), ATR (14), EMA (350), MACD (12,26,9).
