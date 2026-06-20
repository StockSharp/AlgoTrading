# Estrategia Heikin Ashi V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta segunda versión del sistema Heikin Ashi añade un filtro EMA. Las operaciones ocurren solo cuando la dirección de la vela Heikin Ashi concuerda con la tendencia definida por la EMA. El filtro ayuda a evitar señales en contra de la tendencia que el enfoque HA puro podría generar.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `HA_Close > HA_Open` y `Close > EMA`
  - **Corto**: `HA_Close < HA_Open` y `Close < EMA`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `EmaLength` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
