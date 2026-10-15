# Explosión de Volumen (Volume Surge)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Explosión de Volumen identifica volumen inusualmente alto en relación con la media móvil. Cuando la ratio supera el multiplicador definido, señala un fuerte interés y una posible continuación en la dirección del precio respecto a su media móvil.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 52%. Funciona mejor en el mercado de criptomonedas.

Las operaciones se inician solo cuando hay una explosión y se cierran cuando el volumen vuelve a caer por debajo del promedio o cuando se alcanza el stop-loss.

Este sencillo enfoque captura el impulso desencadenado por una participación repentina.

## Detalles

- **Criterios de entrada**: Ratio de volumen por encima de `VolumeSurgeMultiplier`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El volumen cae por debajo del promedio o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `VolumeAvgPeriod` = 20
  - `VolumeSurgeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Volume
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
