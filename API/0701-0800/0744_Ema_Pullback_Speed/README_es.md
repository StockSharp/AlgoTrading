# Estrategia EMA de Velocidad de Retroceso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia EMA Pullback Speed utiliza una EMA dinámica que se adapta a la aceleración del precio. Una posición larga se abre cuando el precio regresa a la EMA dinámica durante una tendencia alcista con una reversión alcista y velocidad ascendente suficiente. Una posición corta se abre en condiciones opuestas. Las salidas usan un stop loss basado en ATR y un take profit de porcentaje fijo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio por encima de la EMA dinámica, reversión alcista, precio regresó a la EMA, velocidad positiva, EMA corta por encima de la EMA larga, velocidad ≥ `LongSpeedMin`.
  - **Corto**: Precio por debajo de la EMA dinámica, reversión bajista, precio regresó a la EMA, velocidad negativa, EMA corta por debajo de la EMA larga, velocidad ≤ `ShortSpeedMax`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Stop loss ATR y take profit de porcentaje fijo.
- **Stops**: Stop loss `AtrMultiplier`×ATR, take profit `FixedTpPct`%.
- **Valores predeterminados**:
  - `MaxLength` = 50
  - `AccelMultiplier` = 3
  - `ReturnThreshold` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 4
  - `FixedTpPct` = 1.5
  - `ShortEmaLength` = 21
  - `LongEmaLength` = 50
  - `LongSpeedMin` = 1000
  - `ShortSpeedMax` = -1000
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Stop loss ATR, take profit fijo
  - Complejidad: Medio
  - Marco temporal: 5m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
