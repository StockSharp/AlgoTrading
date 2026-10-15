# Estrategia Rally Base Drop SND Pivots
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Rally Base Drop SND Pivots opera rupturas de niveles de oferta y demanda basados en pivotes. Los pivotes se detectan cuando secuencias de velas alcistas y bajistas forman patrones rally-base-drop o drop-base-rally. Cuando el precio cruza estos niveles de pivote, se abre una posición. Las salidas usan un stop basado en ATR y un objetivo de riesgo-recompensa.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cruza por encima de un pivote alto (o del pivote bajo cuando se invierte).
  - **Corto**: El precio cruza por debajo de un pivote bajo (o del pivote alto cuando se invierte).
- **Largo/Corto**: Configurable (solo largos, solo cortos o ambos).
- **Criterios de salida**:
  - El precio alcanza el stop ATR o el objetivo de riesgo-recompensa.
- **Stops**: Multiplicador ATR con objetivo de riesgo-recompensa.
- **Valores predeterminados**:
  - `Length` = 3
  - `Mult` = 1.0
  - `RiskReward` = 6.0
  - `ReverseConditions` = false
- **Filtros**:
  - Categoría: Ruptura de soporte/resistencia
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
