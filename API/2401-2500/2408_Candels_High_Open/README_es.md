# Estrategia Candels High Open
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera cuando una vela abre exactamente en su máximo o mínimo.
Se abre una posición larga si la apertura de la vela es igual a su mínimo, anticipando un movimiento alcista.
Se abre una posición corta si la apertura de la vela es igual a su máximo, esperando una caída.
La posición se cierra cuando el precio cruza el valor del Parabolic SAR, que actúa como salida trailing.

## Detalles

- **Criterios de entrada**:
  - Largo: `Open == Low`
  - Corto: `Open == High`
- **Largo/Corto**: Ambos
- **Criterios de salida**: El precio cruza el Parabolic SAR o señal opuesta
- **Stops**: Usa niveles fijos de stop loss y take profit
- **Valores predeterminados**:
  - `StopLevel` = 50m
  - `TakeLevel` = 50m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `ReverseSignals` = false
- **Filtros**:
  - Categoría: Acción del precio
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
