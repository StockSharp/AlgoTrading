# Estrategia HOD/LOD/PMH/PML/PDH/PDL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de niveles del pre-mercado y del día anterior.
Las entradas largas ocurren cuando el precio cruza por encima del máximo del pre-mercado o del día anterior.
Las entradas cortas ocurren cuando el precio cruza por debajo del mínimo del pre-mercado o del día anterior.
Las posiciones se cierran cuando el precio alcanza el máximo o mínimo del día actual.

## Detalles

- **Criterios de entrada**: precio cruzando los niveles del pre-mercado o del día anterior
- **Largo/Corto**: Ambos
- **Criterios de salida**: alcanzar el máximo o mínimo del día actual
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Niveles
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
