# Estrategia Magic Wand STSM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de seguimiento de tendencia que utiliza el indicador Supertrend con filtro SMA de 200 períodos. Opera en la dirección del Supertrend y usa la línea como stop, apuntando a un take profit con relación riesgo/recompensa configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Supertrend por debajo del precio y cierre por encima de SMA200.
  - **Corto**: Supertrend por encima del precio y cierre por debajo de SMA200.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Take profit en `entry ± (entry - Supertrend) * RiskReward`.
  - Stop loss en Supertrend.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Supertrend Period` = 10
  - `Supertrend Multiplier` = 3
  - `MA Length` = 200
  - `Risk Reward` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
