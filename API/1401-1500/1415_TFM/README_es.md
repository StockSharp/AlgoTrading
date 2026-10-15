# Estrategia TFM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura con multiplicador de marco temporal. Utiliza un marco temporal superior formado multiplicando el marco temporal base. Largo cuando el precio rompe por encima del máximo anterior y opcionalmente corto o salida cuando el precio cae por debajo del mínimo anterior.

## Detalles
- **Criterios de entrada**: El precio cruza niveles del marco temporal multiplicado.
- **Largo/Corto**: Largo con corto opcional.
- **Criterios de salida**: Cruce del nivel opuesto o reversión opcional.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleTime` = TimeSpan.FromMinutes(1)
  - `Multiplier` = 2
  - `AllowShort` = false
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos (si los cortos están habilitados)
  - Indicadores: High/Low
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
