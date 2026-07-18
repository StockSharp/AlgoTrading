# Media Móvil Separada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Construye medias móviles separadas para los cierres alcistas y bajistas. Una posición larga se abre cuando la media alcista sube por encima de la bajista, y una posición corta se abre en el cruce inverso. La estrategia soporta SMA, EMA o HMA y puede operar con precios Heikin Ashi.

## Detalles

- **Criterios de entrada**: Media alcista cruzando por encima de la media bajista.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `MaType` = MaType.SMA
  - `Length` = 20
  - `UseHeikinAshi` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA, HMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

