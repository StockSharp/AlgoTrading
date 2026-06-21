# Estrategia XAUUSD de 10 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera XAUUSD en velas de 10 minutos usando señales de MACD, RSI y Bollinger Bands. Abre posiciones largas cuando aparecen condiciones alcistas y posiciones cortas cuando se activan señales bajistas. El sistema aplica niveles de stop-loss y take-profit basados en ATR ajustados por un spread fijo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La línea MACD cruza por encima de la señal, RSI por debajo de sobreventa o precio por debajo de la banda inferior de Bollinger.
  - **Corto**: La línea MACD cruza por debajo de la señal, RSI por encima de sobrecompra o precio por encima de la banda superior de Bollinger.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Posición cerrada en señal contraria, stop-loss o take-profit.
- **Stops**: Stop-loss ATR en `3 * ATR`, take-profit en `5 * ATR`.
- **Valores predeterminados**:
  - MACD fast/slow/signal: `12/26/9`.
  - RSI period: `14`, overbought `65`, oversold `35`.
  - Bollinger length `20`, width `2`.
  - ATR period `14`.
  - Spread `38` ticks.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
