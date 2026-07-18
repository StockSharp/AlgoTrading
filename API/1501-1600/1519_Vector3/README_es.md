# Estrategia Vector3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera basándose en la alineación de tres medias móviles.
Va largo cuando fast > middle > slow y corto cuando fast < middle < slow.

## Detalles

- **Criterios de entrada**: fast MA por encima de middle y middle por encima de slow (largo); inverso para corto
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `FastLength` = 10
  - `MiddleLength` = 50
  - `SlowLength` = 100
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
