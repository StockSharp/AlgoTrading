# Estrategia Geo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compra cuando la relación máximo/mínimo de la vela está cerca de la proporción áurea.

## Detalles

- **Criterios de entrada**: Relación máximo/mínimo dentro de la tolerancia de phi.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Condición opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Tolerance` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candle ratio
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
