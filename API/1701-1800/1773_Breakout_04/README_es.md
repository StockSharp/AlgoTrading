# Estrategia de Ruptura 04
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera rupturas del rango del día anterior.
Compra cuando el precio supera el máximo del día previo y vende cuando cae por debajo del mínimo del día previo.
Utiliza un trailing stop y un take-profit fijo con dimensionamiento de posición opcional basado en el saldo de la cuenta.
La operativa se deshabilita antes de una hora de inicio del lunes configurada y después de una hora de corte del viernes.

## Detalles

- **Criterios de entrada**:
  - Largo: `Precio > Máximo anterior`
  - Corto: `Precio < Mínimo anterior`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Trailing stop o take-profit
- **Stops**: Trailing y stop loss fijo
- **Valores predeterminados**:
  - `MondayHour` = 18
  - `FridayHour` = 14
  - `TrailingStop` = 21
  - `TakeProfit` = 550
  - `StopLoss` = 124
  - `UseMoneyManagement` = false
  - `PercentMM` = 8m
  - `Volume` = 0.1m
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
