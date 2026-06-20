# Estrategia Javo v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Javo v1 combina velas Heikin Ashi con un par de medias móviles exponenciales. Se abre una posición cuando la dirección de HA y el cruce del EMA rápido/lento se alinean. El enfoque intenta captar tendencias emergentes mientras suaviza el ruido.

## Detalles

- **Criterios de entrada**:
  - **Largo**: HA alcista y `EMA_fast > EMA_slow`
  - **Corto**: HA bajista y `EMA_fast < EMA_slow`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `FastEmaPeriod` = 1
  - `SlowEmaPeriod` = 30
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Por hora
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
