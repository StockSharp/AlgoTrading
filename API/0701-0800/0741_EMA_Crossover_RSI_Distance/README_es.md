# Estrategia de Cruce EMA con RSI y Distancia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza múltiples EMA y RSI para generar señales largas y cortas, verificando la distancia entre las EMA rápidas para confirmar la fortaleza de la tendencia.

## Detalles

- **Criterios de entrada**:
  - EMA5 por encima de EMA13.
  - EMA40 por encima de EMA55.
  - RSI por encima de 50 y por encima de su SMA.
  - La distancia entre EMA5 y EMA13 por encima de su promedio y la distancia EMA40-EMA13 en aumento.
  - Precio de cierre por encima de EMA5.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - La señal cambia a neutral o dirección opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `EmaShortLength` = 5
  - `EmaMediumLength` = 13
  - `EmaLong1Length` = 40
  - `EmaLong2Length` = 55
  - `RsiLength` = 14
  - `RsiAverageLength` = 14
  - `DistanceLength` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
