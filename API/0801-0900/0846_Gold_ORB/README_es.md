# Estrategia de ORB del Oro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia captura el máximo y mínimo de la sesión asiática y opera rupturas durante las horas siguientes. Los stops y objetivos se derivan del tamaño del rango con un multiplicador de recompensa.

## Detalles

- **Criterios de entrada**:
  - Durante la ventana de trading, ir largo cuando el precio cierre por encima del máximo asiático registrado.
  - Ir corto cuando el precio cierre por debajo del mínimo asiático registrado.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop y objetivo basados en el tamaño del rango y el multiplicador de recompensa.
- **Stops**: Sí
- **Valores predeterminados**:
  - `AsiaStart` = 00:00
  - `AsiaEnd` = 06:00
  - `TradeStart` = 06:00
  - `TradeEnd` = 10:00
  - `RewardMultiplier` = 2.0
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: 5m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

