# Estrategia Anomalous Holonomy Field Theory
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina EMA, RSI, MACD, ATR, tasa de cambio y distancia al VWAP en una señal compuesta. Las posiciones largas se abren cuando la señal supera un umbral definido por el usuario, mientras que las posiciones cortas se abren cuando cae por debajo del umbral negativo. Un stop basado en ATR protege las operaciones abiertas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: señal ≥ umbral.
  - **Corto**: señal ≤ −umbral.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop ATR.
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `SignalThreshold` = 2
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
