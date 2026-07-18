# Estrategia de Relleno de Brechas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Relleno de Brechas aprovecha las brechas de precio entre velas consecutivas de 15 minutos.
Cuando una nueva vela abre por encima del máximo de la vela anterior en más de un umbral configurable, la estrategia vende y coloca un límite de compra en el máximo anterior, esperando que la brecha se cierre.
Cuando una vela abre por debajo del mínimo anterior en más del umbral, compra y coloca un límite de venta en el mínimo anterior.
El umbral se calcula como `MinGapSize` pasos de precio más el diferencial actual entre la mejor oferta y demanda.

## Detalles

- **Criterios de entrada**: La brecha entre la apertura actual y el máximo/mínimo anterior supera `MinGapSize` más el diferencial.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Orden limitada en el extremo de la vela anterior.
- **Stops**: No.
- **Valores predeterminados**:
  - `MinGapSize` = 1
  - `Volume` = 0.1
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoría: Brecha
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
