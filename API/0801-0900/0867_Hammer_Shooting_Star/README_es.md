# Estrategia Martillo y Estrella Fugaz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera los patrones de velas Hammer y Shooting Star.
Se abre una posición larga cuando la vela anterior es un Hammer,
mientras que una posición corta sigue a una Shooting Star.
Las salidas utilizan el máximo y mínimo de la vela de señal como take profit y stop loss.

## Detalles

- **Criterios de entrada**:
  - Largo: la vela anterior es un Hammer
  - Corto: la vela anterior es una Shooting Star
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss y take profit en el mínimo/máximo de la vela de señal
- **Stops**: Sí, fijos en los extremos de la vela de señal
- **Valores predeterminados**:
  - `WickFactor` = 0.9
  - `MaxOppositeWickFactor` = 0.45
  - `MinBodyRangePct` = 0.2
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Velas japonesas
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
