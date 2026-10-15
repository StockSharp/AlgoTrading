# Estrategia de Z-Score Estadístico de Precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el cruce del Z-Score suavizado con un filtro de impulso de velas.

Compra cuando el Z-Score de corto plazo sube por encima del Z-Score de largo plazo y cierra cuando cae por debajo. La estrategia ignora señales después de varias señales idénticas consecutivas y evita entradas tras tres velas alcistas.

## Detalles

- **Criterios de entrada**: Z-Score de corto plazo por encima del largo plazo, sin secuencia alcista previa de 3 barras, separación entre señales.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Z-Score de corto plazo por debajo del largo plazo, sin secuencia bajista previa de 3 barras, separación entre señales.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZScoreBasePeriod` = 3
  - `ShortSmoothPeriod` = 3
  - `LongSmoothPeriod` = 5
  - `GapBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
