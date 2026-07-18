# Estrategia Logarítmica Intelle city World Cycle ATH ATL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza medias móviles escaladas para marcar señales en el máximo histórico (ATH) y el mínimo histórico (ATL) basándose en el concepto Pi Cycle.

El sistema vende cuando la media móvil larga escalada del ATH cruza por debajo de la media corta, y compra cuando la media móvil larga escalada del ATL cruza por encima de la media corta.

## Detalles

- **Criterios de entrada**: La SMA larga escalada del ATH cruza por debajo de la SMA corta del ATH para vender. La SMA larga escalada del ATL cruza por encima de la SMA corta del ATL para comprar.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `AthLongLength` = 350
  - `AthShortLength` = 111
  - `AtlLongLength` = 471
  - `AtlShortLength` = 150
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
