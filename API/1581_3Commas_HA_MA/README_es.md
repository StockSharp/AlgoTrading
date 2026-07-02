# Estrategia 3Commas HA & MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza velas Heikin Ashi y un par de medias móviles exponenciales. Una operación larga ocurre cuando una vela HA bajista es seguida por una alcista mientras la MA rápida está por encima de la MA lenta. Los cortos siguen la configuración opuesta. Las posiciones se cierran cuando el precio cruza la MA lenta o alcanza el stop de swing.

## Detalles
- **Criterios de entrada**: Reversión de Heikin Ashi con confirmación de MA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza la MA lenta o stop.
- **Stops**: Máximo/mínimo del swing.
- **Valores predeterminados**:
  - `MaFast` = 9
  - `MaSlow` = 18
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
