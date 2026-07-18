# Estrategia 3Commas Turtle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura simplificado al estilo Turtle usando canales Donchian. Compra en rupturas por encima del canal rápido cuando el precio también está por encima del canal lento, y vende en caídas por debajo del canal rápido con confirmación del canal lento. Las salidas ocurren cuando el precio cruza el canal de salida en la dirección opuesta.

## Detalles
- **Criterios de entrada**: Ruptura del canal rápido con confirmación del canal lento.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza el canal de salida.
- **Stops**: Basados en el canal.
- **Valores predeterminados**:
  - `PeriodFast` = 20
  - `PeriodSlow` = 20
  - `PeriodExit` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Canales Donchian
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
