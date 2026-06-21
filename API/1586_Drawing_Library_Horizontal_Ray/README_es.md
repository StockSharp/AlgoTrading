# Estrategia de Rayo Horizontal de Biblioteca de Dibujo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dibuja rayos horizontales en los puntos de cruce de SMA y opera en la dirección del cruce.

## Detalles

- **Criterios de entrada**: `SMA20` cruzando `SMA50` hacia arriba para largo, hacia abajo para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `CandleType` = 5 minutes
- **Filtros**:
  - Categoría: Dibujo
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
