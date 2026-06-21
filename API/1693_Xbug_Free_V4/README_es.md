# Estrategia Xbug Free V4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre posiciones cuando una media móvil del precio medio cruza el propio precio medio. Se colocan un take profit y un stop loss simétricos a una distancia fija del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - Largo: la media móvil está por encima del precio medio y estaba por debajo dos velas atrás
  - Corto: la media móvil está por debajo del precio medio y estaba por encima dos velas atrás
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Take profit a distancia `StopPoints` por encima/debajo de la entrada
  - Stop loss a distancia `StopPoints` en el lado opuesto
- **Stops**: Sí
- **Valores predeterminados**:
  - `MaPeriod` = 19
  - `StopPoints` = 270
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Crossover
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
