# Estrategia AI Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia AI Volume busca estallidos repentinos de participación. Un pico de volumen ocurre cuando el volumen actual supera su EMA por un multiplicador dado. Si el pico se alinea con la EMA de precio de 50 períodos y el color de la vela, la estrategia entra en esa dirección. Cada operación se cierra después de un número fijo de barras.

## Detalles

- **Criterios de entrada**: Volumen > VolumeEMA * VolumeMultiplier y precio por encima/debajo de la EMA 50 con color de vela coincidente.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Posición cerrada después de `ExitBars` velas.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `VolumeEmaLength` = 20
  - `VolumeMultiplier` = 2.0
  - `ExitBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura de volumen
  - Dirección: Ambos
  - Indicadores: EMA, Volume EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
