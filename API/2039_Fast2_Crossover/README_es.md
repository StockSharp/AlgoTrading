# Estrategia de Cruce Fast2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el histograma Fast2. El histograma combina el cuerpo de los últimos tres velas con pesos de raíz cuadrada y aplica dos medias móviles ponderadas. Se abre una posición larga cuando la media rápida cruza por debajo de la media lenta, y una posición corta cuando cruza por encima.

## Detalles

- **Criterios de entrada**:
  - Largo: la WMA rápida cruza por debajo de la WMA lenta
  - Corto: la WMA rápida cruza por encima de la WMA lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cruce opuesto
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `FastLength` = 3
  - `SlowLength` = 9
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filtros**:
  - Categoría: Cruce
  - Dirección: Ambos
  - Indicadores: WeightedMovingAverage
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
