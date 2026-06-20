# Estrategia de Estacionalidad BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición basada en reglas predefinidas de día de la semana y hora usando la Hora Estándar del Este (EST). El usuario elige el día y hora de entrada, el día y hora de salida, y si operar en largo o en corto. La posición se abre en el momento de entrada especificado y se cierra en el momento de salida especificado.

## Detalles

- **Criterios de entrada**:
  - El día EST actual es igual a `EntryDay` y la hora actual es igual a `EntryHour`.
- **Largo/Corto**: Configurable.
- **Criterios de salida**:
  - El día EST actual es igual a `ExitDay` y la hora actual es igual a `ExitHour`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `EntryDay` = Saturday
  - `ExitDay` = Monday
  - `EntryHour` = 10
  - `ExitHour` = 10
  - `IsLong` = true
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Configurable
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
