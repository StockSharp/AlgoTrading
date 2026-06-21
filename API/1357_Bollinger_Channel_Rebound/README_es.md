# Estrategia de Rebote en Canal Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del script de TradingView "strategy1". La estrategia opera rebotes en el canal Bollinger. Entra en largo después de que el precio cae por debajo de la banda inferior y luego cierra por encima de ella. Las salidas se activan al cruzar por encima de la banda media, tocar la banda superior o por stop-loss por debajo del canal.

## Detalles

- **Criterios de entrada**: El precio estaba por debajo de la banda inferior y luego cierra por encima de ella.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cruce por encima de la banda media, toque de la banda superior o stop-loss por debajo del canal.
- **Stops**: Sí, stop fijo por debajo del canal.
- **Valores predeterminados**:
  - `Length` = 20
  - `BufferFactor` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Variable
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
