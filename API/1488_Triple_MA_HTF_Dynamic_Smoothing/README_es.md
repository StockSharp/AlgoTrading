# Estrategia Triple MA HTF - Suavizado Dinámico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compara tres medias móviles calculadas en marcos temporales superiores.
Cada MA de marco temporal superior se suaviza proporcionalmente a la relación entre su marco temporal y el marco temporal de trabajo.
Las señales se generan cuando la primera MA cruza la segunda mientras la tercera confirma la dirección.

## Detalles

- **Criterios de entrada**: Cruce de MA1 y MA2 con confirmación de tendencia de MA3.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HigherTimeFrame1` = TimeSpan.FromMinutes(15)
  - `HigherTimeFrame2` = TimeSpan.FromMinutes(60)
  - `HigherTimeFrame3` = TimeSpan.FromMinutes(240)
  - `Length1` = 21
  - `Length2` = 21
  - `Length3` = 50
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: Ninguno
  - Complejidad: Intermedio
  - Marco temporal: Intradía (base 5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
