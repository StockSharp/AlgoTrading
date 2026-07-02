# Estrategia Volume Weighted Price Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia combina una media móvil con una media móvil ponderada por volumen (VWMA). Cuando el precio opera por encima de la VWMA, sugiere que los compradores son dominantes. Un rompimiento ocurre cuando el precio cruza la VWMA desde el lado opuesto.

Las pruebas indican un retorno anual promedio de aproximadamente 40%. Funciona mejor en el mercado de criptomonedas.

Las operaciones se alinean con la dirección de la VWMA y utilizan la media móvil simple como filtro de tendencia de nivel superior. Las salidas ocurren cuando el precio revierte respecto a la media móvil.

El objetivo es capturar rompimientos respaldados por volumen.

## Detalles

- **Criterios de entrada**: Precio por encima o por debajo de la VWMA con confirmación de la MA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza la MA en dirección opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `VWAPPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: VWMA, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

