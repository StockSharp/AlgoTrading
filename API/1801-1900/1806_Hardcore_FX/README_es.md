# Ruptura Hardcore FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptación del experto MetaTrader "HardcoreFX". La estrategia rastrea los máximos y mínimos de los pivotes de ZigZag y abre posiciones cuando el precio los rompe. Aplica niveles fijos de stop loss y take profit y también realiza un trailing de la posición para proteger las ganancias acumuladas.

## Detalles
- **Criterios de entrada**: Cierre por encima del último máximo de ZigZag para ir largo; cierre por debajo del último mínimo de ZigZag para ir corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Activación de stop loss, take profit o stop trailing.
- **Stops**: Stop loss fijo, take profit y stop trailing.
- **Valores predeterminados**:
  - `ZigzagLength` = 17
  - `StopLoss` = 1400
  - `TakeProfit` = 5400
  - `TrailingStop` = 500
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Stop loss, Take profit, Stop trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
