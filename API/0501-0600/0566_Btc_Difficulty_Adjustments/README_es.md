# Estrategia de Ajustes de Dificultad BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ajustes de Dificultad BTC opera basándose en los cambios en la dificultad de minería de Bitcoin. Cuando el modo de umbral está activado, las operaciones se abren solo si el cambio porcentual supera el umbral especificado. Se abre una posición larga en ajustes de dificultad positivos y una posición corta en ajustes negativos.

## Detalles

- **Criterios de entrada**:
  - Modo umbral: `abs(change) >= Threshold` y `change < 0` → entrar en largo.
  - Modo umbral: `abs(change) >= Threshold` y `change > 0` → entrar en corto.
  - Sin modo umbral: `difficulty > difficulty anterior` → entrar en largo.
  - Sin modo umbral: `difficulty < difficulty anterior` → entrar en corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - La señal opuesta cierra y revierte las posiciones.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `CandleType` = 1 día
  - `ThresholdMode` = false
  - `Threshold` = 10
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Largo y Corto
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
