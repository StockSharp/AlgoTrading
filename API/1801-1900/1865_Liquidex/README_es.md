# Estrategia Liquidex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que entra cuando el precio se mueve fuera de las bandas del Canal de Keltner y gestiona el riesgo con stop loss, take profit, punto de equilibrio y stop dinámico.

## Detalles

- **Criterios de entrada**:
  - Largo: cierre por encima de la banda superior del Canal de Keltner.
  - Corto: cierre por debajo de la banda inferior del Canal de Keltner.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop loss o take profit alcanzado.
  - Stop movido a punto de equilibrio tras alcanzar el objetivo de beneficio.
  - Stop dinámico activado.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `KcPeriod` = 10
  - `UseKcFilter` = true
  - `StopLoss` = 30
  - `TakeProfit` = 0
  - `MoveToBe` = 15
  - `MoveToBeOffset` = 2
  - `TrailingDistance` = 5
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Canal
  - Dirección: Ambos
  - Indicadores: Keltner
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
