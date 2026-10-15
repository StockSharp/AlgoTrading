# Estrategia de Contra-Tendencia por Anomalía
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El algoritmo detecta movimientos porcentuales bruscos en una ventana corta y opera contra ellos. Cuando el precio sube por encima del umbral vende; cuando el precio cae por debajo del umbral compra. El stop-loss y take-profit se establecen en ticks.

## Detalles

- **Criterios de entrada**: El cambio porcentual en la ventana de retroceso supera el umbral.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `PercentageThreshold` = 1
  - `LookbackMinutes` = 30
  - `StopLossTicks` = 100
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Contra-tendencia
  - Dirección: Ambos
  - Indicadores: Precio
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
