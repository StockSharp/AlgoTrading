# Estrategia de Breakeven con Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que demuestra cómo mover el stop loss a breakeven y luego seguirlo conforme el precio avanza.
La estrategia entra en una posición larga y la gestiona en dos fases:
1. Después de que el precio gana `BreakevenPlus` puntos, el stop se mueve a `BreakevenStep` puntos por encima de la entrada.
2. Cuando el precio continúa con `TrailingPlus` puntos de ganancia por encima del stop, el stop sigue al precio a `TrailingStep` puntos de distancia.

La lógica es simétrica para posiciones cortas si se abre una manualmente.

## Detalles

- **Criterios de entrada**: Abre una posición larga en la primera vela completada.
- **Largo/Corto**: Ambos (el ejemplo usa largo).
- **Criterios de salida**: El precio cruza el trailing stop.
- **Stops**: Breakeven y trailing stop.
- **Valores predeterminados**:
  - `BreakevenPlus` = 5
  - `BreakevenStep` = 2
  - `TrailingPlus` = 3
  - `TrailingStep` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Gestión de stops
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Breakeven, trailing
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
