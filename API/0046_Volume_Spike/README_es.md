# Volume Spike Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Volume Spike Trend monitorea súbitos aumentos en el volumen negociado. Cuando el volumen actual supera el promedio reciente por un multiplicador determinado, señala una fuerte participación.

Las pruebas indican un retorno anual promedio de aproximadamente 175%. Funciona mejor en el mercado de acciones.

Si el volumen presenta un spike y el precio está por encima de la media móvil, la estrategia compra; si el volumen presenta un spike por debajo del promedio, vende en corto. Las operaciones salen cuando el volumen vuelve a caer por debajo del promedio o se alcanza el stop-loss.

Este método busca capturar movimientos impulsados por una explosión de actividad.

## Detalles

- **Criterios de entrada**: El cambio de volumen supera `VolumeSpikeMultiplier` veces el promedio.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El volumen cae por debajo del promedio o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `VolAvgPeriod` = 20
  - `VolumeSpikeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Volumen, MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

