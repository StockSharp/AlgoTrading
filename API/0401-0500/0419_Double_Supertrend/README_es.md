# Estrategia Double Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Double Supertrend emplea dos medias móviles basadas en ATR con diferentes períodos
y multiplicadores. La primera línea establece la dirección de la operación, mientras
que la segunda puede actuar como objetivo o salida de seguimiento. Esta combinación
permite un seguimiento de tendencia flexible con parámetros de ganancia y riesgo
definidos.

Cuando el precio se mueve por encima de ambas líneas y la estrategia está configurada
para operar en largo, se abre una posición. Para operaciones en corto, las condiciones
se invierten. Las salidas dependen del tipo de toma de ganancias seleccionado o de un
stop-loss porcentual.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**: El precio cruza las líneas de Supertrend en la `Direction` permitida.
- **Criterios de salida**: Ruptura de la línea opuesta, toma de ganancias (`TPType`/`TPPercent`) o stop-loss (`SLPercent`).
- **Stops**: Stop porcentual basado en `SLPercent`.
- **Valores predeterminados**:
  - `ATRPeriod1` = 10
  - `Factor1` = 3.0
  - `ATRPeriod2` = 20
  - `Factor2` = 5.0
  - `Direction` = "Long"
  - `TPType` = "Supertrend"
  - `TPPercent` = 1.5
  - `SLPercent` = 10.0
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Configurable
  - Indicadores: ATR‑based Supertrend
  - Complejidad: Avanzado
  - Nivel de riesgo: Medio
