# Estrategia ALMA Optimized
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una media móvil Arnaud Legoux con una EMA a largo plazo, ADX, RSI y Bandas de Bollinger. Un filtro basado en ATR asegura volatilidad suficiente. Las posiciones utilizan múltiplos de ATR para el stop-loss y take-profit, con una salida opcional basada en tiempo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ATR por encima del umbral, cierre por encima de EMA y ALMA, RSI > 30, ADX > 30, cierre por debajo de la banda superior de Bollinger y cooldown superado.
  - **Corto**: El cierre cruza por debajo de la EMA rápida bajo el mismo filtro de volatilidad.
- **Criterios de salida**:
  - Stop-loss o take-profit basados en múltiplos de ATR.
  - Salida opcional basada en tiempo en barras.
- **Valores predeterminados**:
  - EMA rápida = 20.
  - Longitud ATR = 14.
  - Longitud EMA = 72.
  - Longitud ADX = 10.
  - Longitud RSI = 14.
  - Cooldown = 7 barras.
  - Multiplicador Bollinger = 3.0.
  - Multiplicador ATR de stop = 5.0.
  - Multiplicador ATR de objetivo = 4.0.
  - Salida temporal = 0.
  - ATR mínimo = 0.005.
- **Filtros**:
  - Categoría: Tendencia + Momentum
  - Dirección: Ambos
  - Indicadores: EMA, ALMA, ADX, RSI, ATR, Bollinger Bands
  - Stops: Basado en ATR
  - Complejidad: Moderado
  - Marco temporal: Corto/medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
