# Estrategia de OPR Binned Filtrado por Sigma Spike
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sigma Spike Filtered Binned OPR recopila la distribución de la proporción de posiciones abiertas (OPR) y opera cuando el OPR alcanza bins extremos tras un pico sigma en los rendimientos.

## Detalles

- **Criterios de entrada**: OPR en bins extremos (<= `OprThreshold` o >= `100 - OprThreshold`) con filtro de pico sigma opcional
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal contraria
- **Stops**: No
- **Valores predeterminados**:
  - `SigmaSpikeLength` = 20
  - `FilterBySigmaSpike` = true
  - `SigmaSpikeThreshold` = 2
  - `OprThreshold` = 10
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
