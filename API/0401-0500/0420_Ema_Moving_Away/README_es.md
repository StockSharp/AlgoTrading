# Estrategia EMA Moving Away
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA Moving Away rastrea cuánto se aleja el precio de una media móvil exponencial.
Cuando una secuencia de velas empuja un porcentaje definido por debajo del EMA, la
estrategia apuesta a un retorno a la media.

El setup se enfoca en el lado largo: después de un movimiento bajista extendido que
lleva el precio por debajo del EMA en `MovingAwayPercent`, se abre una posición.
Los filtros de tamaño del cuerpo y de racha pueden añadirse para asegurar que el
movimiento esté estirado en lugar de ser ruido. Un stop-loss porcentual protege el
capital si la reversión falla.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cierre por debajo del EMA en `MovingAwayPercent` con los filtros de racha/tamaño requeridos.
  - **Corto**: no se utiliza.
- **Criterios de salida**: Retorno al EMA o activación del stop-loss.
- **Stops**: Stop porcentual basado en `StopLossPercent`.
- **Valores predeterminados**:
  - `EmaLength` = 55
  - `MovingAwayPercent` = 2.0
  - `StopLossPercent` = 2.0
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: EMA
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
