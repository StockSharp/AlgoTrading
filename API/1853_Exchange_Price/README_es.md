# Estrategia Exchange Price
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compara el precio de cierre actual con los precios de varias barras atrás durante dos períodos de lookback. Se abre una posición larga cuando el cambio a corto plazo supera al cambio a largo plazo; se abre una posición corta cuando ocurre el cruce opuesto.

## Detalles

- **Criterios de entrada**: diferencia de precio a corto plazo cruzando por encima/por debajo de la diferencia a largo plazo
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce opuesto
- **Stops**: No
- **Valores predeterminados**:
  - `ShortPeriod` = 96
  - `LongPeriod` = 288
  - `CandleType` = velas de 8 horas
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Diferencia de precio
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 8 horas
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
