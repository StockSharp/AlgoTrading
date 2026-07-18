# Estrategia Ehlers SwamiCharts RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Promedia los valores de RSI de los períodos 2–48 para construir un mapa de colores. Largo cuando el color promedio es verde, corto cuando es rojo.

## Detalles

- **Criterios de entrada**: El color promedio es verde (`Color1Avg` == 255 y `Color2Avg` > `LongColor`) para largo; rojo (`Color1Avg` > `ShortColor` y `Color2Avg` == 255) para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `LongColor` = 50
  - `ShortColor` = 50
  - `CandleType` = 5 minutes
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
