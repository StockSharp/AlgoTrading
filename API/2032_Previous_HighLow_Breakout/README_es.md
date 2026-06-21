# Estrategia de Rompimiento del Máximo/Mínimo Previo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de rompimiento que monitorea el máximo y mínimo de la vela anterior en el marco temporal elegido. Se abre una posición larga cuando la nueva vela cierra por encima del máximo previo, mientras que se abre una posición corta cuando el cierre cae por debajo del mínimo previo. Un stop trailing y un take profit fijo gestionan el riesgo y aseguran las ganancias.

El método busca capturar movimientos direccionales fuertes tras la consolidación. Los stops trailing mantienen el riesgo ajustado a medida que el precio se mueve en la dirección favorable.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > PreviousHigh`
  - Corto: `Close < PreviousLow`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop loss o take profit
- **Stops**: Absolutos con trailing usando `StopLoss` y `TakeProfit`
- **Valores predeterminados**:
  - `StopLoss` = 50m
  - `TakeProfit` = 1000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí (trailing)
  - Complejidad: Principiante
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
