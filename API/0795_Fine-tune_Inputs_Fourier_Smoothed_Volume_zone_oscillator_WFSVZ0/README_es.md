# Estrategia de Oscilador de Zona de Volumen Suavizado por Fourier WFSVZ0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza un Oscilador de Zona de Volumen suavizado por Fourier. Abre largo cuando el oscilador sube por encima del umbral y corto cuando cae por debajo del umbral negativo. Opcionalmente cierra posiciones abiertas cuando no hay señal.

## Detalles

- **Criterios de entrada**: Oscilador sube por encima del umbral / cae por debajo del umbral negativo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o cierre opcional de todo.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `VzoLength` = 2
  - `SmoothLength` = 2
  - `Threshold` = 0m
  - `CloseAllPositions` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: Volume Zone Oscillator
  - Stops: Ninguno
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
