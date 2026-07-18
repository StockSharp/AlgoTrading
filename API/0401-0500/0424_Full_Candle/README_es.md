# Estrategia de Full Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La configuración Full Candle entra cuando una vela cierra más allá de su EMA y deja solo una mecha pequeña en el lado del rompimiento. La intención es operar velas de momentum que muestren una acción decisiva sin mucho rechazo. Las salidas opcionales de take-profit y stop-loss basadas en porcentaje gestionan la operación una vez abierta.

El sistema es más adecuado para rompimientos a corto plazo donde las velas fuertes a menudo conducen a una continuación rápida.

## Detalles

- **Criterios de entrada**:
  - **Largo**: vela alcista cerrando por encima de la EMA con sombra ≤ umbral
  - **Corto**: vela bajista cerrando por debajo de la EMA con sombra ≤ umbral
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Porcentajes de take-profit o stop-loss si están habilitados
- **Stops**: Opcional
- **Valores predeterminados**:
  - `EmaLength` = 10
  - `ShadowPercent` = 5
  - `TPPercent` = 1.2
  - `SLPercent` = 1.8
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, price action
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
