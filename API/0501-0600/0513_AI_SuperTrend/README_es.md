# Estrategia AI SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia AI SuperTrend combina el indicador SuperTrend con medias móviles ponderadas del precio y de la línea SuperTrend. Se abre una operación larga cuando el SuperTrend gira hacia arriba y la WMA del precio se mueve por encima de la WMA del SuperTrend. Se abre una operación corta en las condiciones opuestas. Las posiciones están protegidas con un trailing stop dinámico basado en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La dirección de SuperTrend cambia hacia arriba y la WMA del precio está por encima de la WMA del SuperTrend.
  - **Corto**: La dirección de SuperTrend cambia hacia abajo y la WMA del precio está por debajo de la WMA del SuperTrend.
- **Criterios de salida**:
  - Reversión de tendencia o trailing stop ATR.
- **Stops**: Trailing stop ATR dinámico.
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `AtrFactor` = 3
  - `PriceWmaLength` = 20
  - `SuperWmaLength` = 100
  - `EnableLong` = true
  - `EnableShort` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, WMA, ATR
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
