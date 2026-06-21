# Estrategia Entry Fragger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rastrea secuencias de velas rojas y verdes en relación con la EMA de 50 períodos. Después de una serie de velas rojas por debajo de la EMA, una vela verde que cierra por encima de una nube de volatilidad activa una entrada larga. Una configuración similar con velas verdes precede las entradas cortas. El trading inverso opcional permite invertir posiciones.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `redCount >= Buy Signal Accuracy` && última roja por debajo de EMA50 && vela verde cierra por encima de `EMA50 + stdev/4`.
  - **Corto**: `greenCount >= Sell Signal Accuracy` && vela anterior verde && vela roja cierra por encima de `EMA50 + stdev/4`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal inversa.
- **Indicadores**: EMA, StandardDeviation.
- **Valores predeterminados**:
  - `Buy Signal Accuracy` = 2
  - `Sell Signal Accuracy` = 2
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
