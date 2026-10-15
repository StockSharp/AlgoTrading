# Estrategia Lux Clara EMA + VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Lux Clara EMA + VWAP opera el cruce de una EMA rápida y una lenta, filtrado por VWAP y una ventana de tiempo. Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta mientras la EMA lenta está por encima del VWAP durante la sesión. Se abre una posición corta en condiciones opuestas. Las posiciones se cierran cuando las EMA cruzan en dirección contraria.

## Detalles

- **Criterios de entrada**:
  - EMA rápida cruza por encima de la EMA lenta, EMA lenta por encima del VWAP y hora actual dentro de la sesión.
  - Corto: EMA rápida cruza por debajo de la EMA lenta, EMA lenta por debajo del VWAP y hora actual dentro de la sesión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto de EMA.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastEmaLength` = 8
  - `SlowEmaLength` = 50
  - `StartTime` = 07:30
  - `EndTime` = 14:30
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: EMA, VWAP
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
