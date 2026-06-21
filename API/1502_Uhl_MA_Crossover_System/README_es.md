# Sistema Uhl MA Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Sistema Uhl MA Crossover construye dos líneas adaptativas (CTS y CMA) usando varianza para ajustar el suavizado. Se abre una posición larga cuando CTS cruza por encima de CMA y una posición corta cuando cruza por debajo.

## Detalles

- **Criterios de entrada**: CTS cruza por encima de CMA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: CTS cruza por debajo de CMA.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 100
  - `Multiplier` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, Variance
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
