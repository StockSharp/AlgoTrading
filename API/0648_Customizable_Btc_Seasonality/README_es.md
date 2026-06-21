# Estrategia de Estacionalidad BTC Configurable
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia explota la estacionalidad intradía de Bitcoin entrando y saliendo en horas UTC definidas por el usuario.
Se abre una posición larga en la hora de entrada y se cierra en la hora de salida.

## Detalles

- **Criterios de entrada**: el tiempo es igual a la hora de entrada definida por el usuario
- **Largo/Corto**: Solo largos
- **Criterios de salida**: el tiempo es igual a la hora de salida definida por el usuario
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 1 minuto
  - `EntryHour` = 21
  - `ExitHour` = 23
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
