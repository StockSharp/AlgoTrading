# Estrategia de Price Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Price Flip refleja el precio alrededor de máximos y mínimos recientes y opera cruces de medias móviles cuando el cierre anterior se encuentra en el lado opuesto de este precio invertido. Se puede aplicar un filtro de tendencia basado en la media móvil lenta.

## Detalles

- **Criterios de entrada**:
  - El cierre anterior está por encima del precio invertido.
  - La MA rápida cruza por encima de la MA lenta.
  - Opcional: el precio está por encima de la MA lenta cuando el filtro de tendencia está habilitado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal opuesta activa una reversión.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `TickerMaxLookback` = 100
  - `TickerMinLookback` = 100
  - `FastMaLength` = 12
  - `SlowMaLength` = 14
  - `UseTrendFilter` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, Highest/Lowest
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
