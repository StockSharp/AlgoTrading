# Compra en el Mínimo de 5 Días
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Buy On 5 Day Low** abre posiciones largas cuando el cierre cae por debajo del mínimo de los 5 días anteriores. Sale cuando el cierre sube por encima del máximo de la barra anterior. Las operaciones se limitan a una ventana de tiempo configurable.

## Detalles
- **Criterios de entrada**: El cierre cae por debajo del mínimo más bajo de las últimas N velas.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: El cierre supera el máximo anterior.
- **Stops**: No.
- **Valores predeterminados**:
  - `LowestPeriod = 5`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `StartTime = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - `EndTime = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)`
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Lowest, High
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
