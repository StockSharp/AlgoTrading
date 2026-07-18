# Estrategia SAR Sistema de Seguimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra en posiciones largas o cortas aleatorias a intervalos de tiempo fijos y gestiona las salidas usando el indicador Parabolic SAR.
El valor del Parabolic SAR actúa como un stop trailing: la posición se cierra cuando el precio cruza el nivel SAR.

## Detalles

- **Criterios de entrada**:
  - Cada `TimerInterval`, si no hay posición abierta y `UseRandomEntry` está habilitado, se abre una operación larga o corta aleatoria.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Precio cruzando el Parabolic SAR.
- **Stops**: Stop-loss inicial en ticks con salida trailing Parabolic SAR.
- **Valores predeterminados**:
  - `TimerInterval` = 300 segundos
  - `StopLossTicks` = 10
  - `AccelerationStep` = 0.02
  - `AccelerationMax` = 0.2
  - `UseRandomEntry` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
