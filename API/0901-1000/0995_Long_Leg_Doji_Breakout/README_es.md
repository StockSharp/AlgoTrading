# Estrategia de Ruptura Doji de Patas Largas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura Doji de Patas Largas identifica velas Doji de patas largas y opera rupturas por encima o por debajo del rango del Doji. Un filtro ATR opcional garantiza que las mechas sean suficientemente largas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Esperando ruptura && close > máximo del Doji && cierre anterior <= máximo del Doji.
  - **Corto**: Esperando ruptura && close < mínimo del Doji && cierre anterior >= mínimo del Doji.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El cierre cruza la SMA(20) en dirección contraria a la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Doji body threshold %` = 0.1
  - `Minimum wick ratio` = 2
  - `Use ATR filter` = true
  - `ATR period` = 14
  - `ATR multiplier` = 0.5
- **Filtros**:
  - Categoría: Ruptura de patrón
  - Dirección: Ambos
  - Indicadores: ATR, SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
