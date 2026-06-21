# Cruce MACD AUDUSD D1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera AUDUSD en el marco temporal diario utilizando cruces de líneas MACD.

La estrategia abre una posición larga cuando la línea principal del MACD cruza por encima de la línea de señal y una posición corta cuando la cruza por debajo. Las operaciones están permitidas únicamente entre las 06:00 y las 14:00 hora del servidor, y solo puede haber una posición abierta a la vez. Cada operación establece un stop loss de 40 pips y un take profit tres veces mayor por defecto.

## Detalles

- **Criterios de entrada**: La línea principal del MACD cruza la línea de señal.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Volume` = 0.1
  - `StopLossPips` = 40
  - `RewardRatio` = 3
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
