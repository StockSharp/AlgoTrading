# Estrategia Lucky Jump
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Lucky Jump es un sistema de reversión a la media a corto plazo que reacciona a saltos repentinos de precio en el mejor bid y ask. Cuando el precio ask salta hacia arriba un número específico de puntos en comparación con la cotización anterior, la estrategia abre una posición corta esperando un retroceso. Por el contrario, cuando el precio bid cae la misma cantidad, entra en largo. Las posiciones se cierran en el primer tick favorable o cuando la pérdida supera un límite predefinido.

Este enfoque intenta capturar correcciones rápidas después de movimientos agresivos del mercado. Opera puramente sobre datos de cotización Level1 y no depende de velas ni indicadores.

## Detalles

- **Criterios de entrada**:
  - **Corto**: `Ask(t) - Ask(t-1) >= Shift * PriceStep`.
  - **Largo**: `Bid(t-1) - Bid(t) >= Shift * PriceStep`.
- **Criterios de salida**:
  - Cerrar la posición tan pronto como sea rentable.
  - Cerrar si la pérdida supera `Limit * PriceStep`.
- **Stops**: stop implícito basado en el parámetro `Limit`.
- **Valores predeterminados**:
  - `Shift` = 30 puntos.
  - `Limit` = 180 puntos.
  - `Volume` = 1.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Ultra corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto

