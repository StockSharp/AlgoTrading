# Estrategia ICT Master Suite Trading IQ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia ICT Master Suite opera rupturas del máximo y mínimo de la sesión diaria. Cuando el precio cierra por encima del máximo de la sesión, la estrategia entra en una posición larga; cuando el precio cierra por debajo del mínimo de la sesión, entra en una posición corta. Las posiciones se gestionan con un stop trailing basado en ATR.

## Detalles

- **Criterios de entrada**:
  - El precio cierra por encima del máximo de la sesión actual (largo).
  - El precio cierra por debajo del mínimo de la sesión actual (corto).
- **Largo/Corto**: Largo y Corto.
- **Criterios de salida**:
  - Stop trailing basado en ATR.
- **Stops**: Stop trailing por ATR.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `AllowLong` = true
  - `AllowShort` = true
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
