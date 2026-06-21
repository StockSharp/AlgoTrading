# Estrategia de Rango Estrecho
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas después de una barra interior donde el rango de la vela más reciente es más estrecho que el de la barra de referencia `Length` períodos atrás. Se colocan órdenes stop en el máximo y mínimo de referencia con un take profit igual al rango de referencia y un stop loss establecido como porcentaje de dicho rango.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio supera el máximo de referencia tras una barra de rango estrecho
  - Corto: el precio rompe por debajo del mínimo de referencia tras una barra de rango estrecho
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Take profit en el rango de referencia
  - Stop loss como porcentaje del rango
- **Stops**: Sí
- **Valores predeterminados**:
  - `Length` = 4
  - `StopLossPercent` = 0.35m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
