# Estrategia de Ruptura de Máximos y Mínimos con Análisis Estadístico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera rupturas de los niveles de máximo o mínimo del marco temporal seleccionado. La estrategia puede entrar en largo o corto según la opción configurada y cierra la posición después de un número fijo de barras.

## Detalles

- **Criterios de entrada**: El cierre cruza el nivel de máximo o mínimo seleccionado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o después de HoldingPeriod barras.
- **Stops**: No.
- **Valores predeterminados**:
  - `EntryOption` = LongAtHigh
  - `TimeframeOption` = Daily
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: High, Low
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
