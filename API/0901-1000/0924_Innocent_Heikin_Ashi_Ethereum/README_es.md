# Estrategia Innocent Heikin Ashi Ethereum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra Ethereum cuando una secuencia de velas bajistas por debajo de la EMA50 es seguida por una vela alcista por encima de la EMA50. El stop loss se coloca en el mínimo más bajo de las últimas 28 barras y el take profit se calcula con el multiplicador `RiskReward`. El **Moon Mode** opcional permite entradas por encima de la EMA200. La posición puede cerrar anticipadamente ante señales de venta o de trampa.

## Detalles

- **Criterios de entrada**:
  - **Largo**: al menos `ConfirmationLevel` velas rojas por debajo de la EMA50, seguidas de una vela verde por encima de la EMA50.
  - **Agresivo**: si `EnableMoonMode` es verdadero y el precio está por encima de la EMA200.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**:
  - Stop loss en el mínimo más bajo de las últimas 28 barras.
  - Take profit usando el multiplicador `RiskReward`.
  - Señales opcionales de venta o trampa para salida anticipada.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RiskReward` = 1.
  - `ConfirmationLevel` = 1.
  - `EnableMoonMode` = true.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
