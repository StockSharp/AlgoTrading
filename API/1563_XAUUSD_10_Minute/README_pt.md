# Estratégia XAUUSD de 10 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera XAUUSD em velas de 10 minutos usando sinais de MACD, RSI e Bollinger Bands. Abre posições compradas quando surgem condições altistas e posições vendidas quando sinais baixistas são acionados. O sistema aplica níveis de stop-loss e take-profit baseados em ATR ajustados por um spread fixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A linha MACD cruza acima da sinal, RSI abaixo de sobrevenda ou preço abaixo da banda inferior de Bollinger.
  - **Vendido**: A linha MACD cruza abaixo da sinal, RSI acima de sobrecompra ou preço acima da banda superior de Bollinger.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Posição fechada em sinal contrário, stop-loss ou take-profit.
- **Stops**: Stop-loss ATR em `3 * ATR`, take-profit em `5 * ATR`.
- **Valores padrão**:
  - MACD fast/slow/signal: `12/26/9`.
  - RSI period: `14`, overbought `65`, oversold `35`.
  - Bollinger length `20`, width `2`.
  - ATR period `14`.
  - Spread `38` ticks.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
