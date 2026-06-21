# Estrategia MTrainer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia MTrainer replica el script MTrainer de MT4. Abre una posición cuando el precio alcanza una línea de entrada predefinida y la gestiona con stop-loss, take-profit y líneas opcionales de cierre parcial. La estrategia está diseñada para la práctica manual en el probador visual.

## Detalles

- **Criterios de entrada**: el precio cruza la línea de entrada
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss, take profit o cierre parcial
- **Stops**: Sí
- **Valores predeterminados**:
  - `EntryPrice` = 0
  - `TakeProfitPrice` = 0
  - `StopLossPrice` = 0
  - `PartialClosePercent` = 0
  - `PartialClosePrice` = 0
  - `Volume` = 1
- **Filtros**:
  - Categoría: Utilidad
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
