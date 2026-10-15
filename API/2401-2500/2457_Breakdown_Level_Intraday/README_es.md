# Estrategia de Ruptura de Nivel Intradía
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Coloca órdenes de ruptura alrededor del máximo y mínimo del día anterior a una hora especificada. Entra en largo cuando el precio cruza por encima del máximo más un delta y en corto cuando el precio cae por debajo del mínimo menos el delta. La gestión de posiciones incluye stop loss opcional, toma de beneficios, break-even y trailing stop.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cruza por encima del máximo del día anterior + `Delta`
  - Corto: el precio cruza por debajo del mínimo del día anterior − `Delta`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop loss o toma de beneficios alcanzados
  - Activación del trailing stop o ajuste de break-even
- **Stops**: Puntos desde el precio de entrada
- **Valores predeterminados**:
  - `OrderTime` = TimeSpan.Zero
  - `Delta` = 6
  - `StopLoss` = 120
  - `TakeProfit` = 90
  - `NoLoss` = 0
  - `Trailing` = 0
  - `Volume` = 1m
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
