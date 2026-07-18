# Estrategia de Momentum BTCUSD Tras Días Anormales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia mide el rendimiento diario como `(close - open) / open` y lo compara con una media móvil y una desviación estándar sobre un período configurable. Si el rendimiento supera el umbral superior, abre una posición larga; si cae por debajo del umbral inferior, abre una posición corta. Todas las posiciones se cierran al cierre del día siguiente.

## Detalles

- **Criterios de entrada**:
  - Rendimiento > media + k × std → largo.
  - Rendimiento < media - k × std → corto.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cerrar todas las posiciones al cierre del día siguiente.
- **Stops**: Ninguno
- **Valores predeterminados**:
  - Período de lookback = 5
  - Umbral de rendimiento anormal (k) = 1.6
  - Capital por operación = 1000
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
