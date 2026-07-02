# Estrategia Fracture
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Fracture combina rupturas por fractales con medias móviles suavizadas y ADX para operar tanto en mercados laterales como en tendencia.

## Detalles

- **Criterios de entrada**: Si el ADX está por debajo del umbral, ir largo por encima del último fractal alcista o corto por debajo del último fractal bajista cuando el precio también esté por encima/debajo de la SMMA rápida. En régimen de tendencia (SMMA rápida por encima/debajo de las más lentas), entrar en la dirección de la tendencia al cruzar el precio la SMMA rápida.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cerrar posición una vez que el beneficio supera el ATR multiplicado por `MinProfit`.
- **Stops**: Objetivo de beneficio basado en ATR.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `AtrPeriod` = 14
  - `AdxPeriod` = 22
  - `AdxLine` = 40
  - `Ma1Period` = 5
  - `Ma2Period` = 9
  - `Ma3Period` = 22
  - `RangingMultiplier` = 0.5
  - `MinProfit` = 1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo y Corto
  - Indicadores: Fractal, SMMA, ATR, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
