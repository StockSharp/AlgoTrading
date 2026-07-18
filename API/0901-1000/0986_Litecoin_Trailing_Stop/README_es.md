# Estrategia de Stop Trailing para Litecoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Stop Trailing para Litecoin** usa la Media Móvil Adaptativa de Kaufman (KAMA) para detectar tendencias alcistas y bajistas. Abre posiciones largas cuando KAMA sube y posiciones cortas cuando baja. Tras un retraso configurable, un stop trailing basado en porcentaje protege las ganancias.

## Detalles
- **Criterios de entrada**: Pendiente de KAMA con enfriamiento entre entradas.
- **Largo/Corto**: ambas direcciones.
- **Criterios de salida**: stop trailing.
- **Stops**: stop trailing tras el retraso.
- **Valores predeterminados**:
  - `KamaLength = 50`
  - `BarsBetweenEntries = 30`
  - `TrailingStopPercent = 12`
  - `DelayBars = 50`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: KAMA
  - Stops: Trailing
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
